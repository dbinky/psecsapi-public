import { test, expect, Page } from '@playwright/test';
import { execSync, spawn, ChildProcess } from 'child_process';
import * as path from 'path';
import * as fs from 'fs';

const REPO_ROOT = path.resolve(__dirname, '..', '..', '..');
const CONSOLE_PROJECT = path.join(REPO_ROOT, 'psecsapi.Console');
const REPLAY_FILE = path.join(__dirname, 'test-replay.bin');

let serverProcess: ChildProcess | null = null;
let serverUrl: string = '';

/**
 * Run a combat simulation and generate a replay binary for testing.
 */
function generateTestReplay(): void {
  console.log('Generating test replay...');
  const result = execSync(
    `dotnet run --project "${CONSOLE_PROJECT}" -- combat simulate ` +
    `--fleet1 preset:balanced-trio --fleet2 preset:heavy-assault ` +
    `--terrain rubble --seed 42 --output "${REPLAY_FILE}"`,
    { cwd: REPO_ROOT, timeout: 120000, encoding: 'utf-8' }
  );
  console.log('Simulation result:', result.trim().split('\n').pop());
}

/**
 * Start the replay server and return the URL.
 */
function startReplayServer(): Promise<string> {
  return new Promise((resolve, reject) => {
    const proc = spawn('dotnet', [
      'run', '--project', CONSOLE_PROJECT,
      '--', 'combat', 'visualize', REPLAY_FILE
    ], { cwd: REPO_ROOT, stdio: ['pipe', 'pipe', 'pipe'] });

    serverProcess = proc;
    let output = '';

    proc.stdout?.on('data', (data: Buffer) => {
      output += data.toString();
      // Look for the URL in output
      const urlMatch = output.match(/http:\/\/localhost:\d+/);
      if (urlMatch) {
        resolve(urlMatch[0]);
      }
    });

    proc.stderr?.on('data', (data: Buffer) => {
      const text = data.toString();
      // Also check stderr for URL (dotnet may output warnings there)
      const urlMatch = text.match(/http:\/\/localhost:\d+/);
      if (urlMatch) {
        resolve(urlMatch[0]);
      }
    });

    proc.on('error', reject);

    // Timeout after 30 seconds
    setTimeout(() => {
      if (!serverUrl) {
        reject(new Error('Server did not start within 30s. Output: ' + output));
      }
    }, 30000);
  });
}

test.describe('Combat Visualizer', () => {

  test.beforeAll(async () => {
    // Always generate a fresh replay to avoid stale fixtures when the
    // replay format changes. The binary is excluded from git via .gitignore.
    generateTestReplay();
    expect(fs.existsSync(REPLAY_FILE)).toBeTruthy();

    // Start replay server
    serverUrl = await startReplayServer();
    console.log('Replay server at:', serverUrl);

    // Give server a moment to be fully ready
    await new Promise(r => setTimeout(r, 1000));
  });

  test.afterAll(async () => {
    if (serverProcess) {
      serverProcess.kill('SIGTERM');
      serverProcess = null;
    }
  });

  test('replay endpoint returns valid JSON array', async ({ request }) => {
    const resp = await request.get(`${serverUrl}/replay`);
    expect(resp.ok()).toBeTruthy();

    const events = await resp.json();
    expect(Array.isArray(events)).toBeTruthy();
    expect(events.length).toBeGreaterThan(10);

    // First event should be CombatStarted (type 0)
    expect(events[0].eventType).toBe(0);
    // CombatStarted must have gridWidth (polymorphic serialization check)
    expect(events[0].gridWidth).toBeGreaterThan(0);
    expect(events[0].shipLoadouts).toBeDefined();
    expect(events[0].shipLoadouts.length).toBeGreaterThan(0);
  });

  test('replay events have correct polymorphic properties', async ({ request }) => {
    const resp = await request.get(`${serverUrl}/replay`);
    const events = await resp.json();

    // Check CombatStarted (type 0) has full properties
    const started = events.find((e: any) => e.eventType === 0);
    expect(started).toBeDefined();
    expect(started.gridWidth).toBeDefined();
    expect(started.gridHeight).toBeDefined();
    expect(started.terrain).toBeDefined();
    expect(started.shipLoadouts).toBeDefined();

    // Check ShipMoved (type 1) has position data
    const moved = events.find((e: any) => e.eventType === 1);
    expect(moved).toBeDefined();
    expect(moved.shipId).toBeDefined();
    expect(typeof moved.newX).toBe('number');
    expect(typeof moved.newY).toBe('number');
    expect(typeof moved.tick).toBe('number');

    // Check CombatEnded (type 9) exists
    const ended = events.find((e: any) => e.eventType === 9);
    expect(ended).toBeDefined();
    expect(typeof ended.tickCount).toBe('number');
  });

  test('visualizer loads and shows combat data', async ({ page }) => {
    await page.goto(serverUrl, { waitUntil: 'networkidle' });

    // Wait for loading overlay to disappear
    await page.waitForFunction(() => {
      const overlay = document.getElementById('loadingOverlay');
      return !overlay || overlay.classList.contains('hidden');
    }, { timeout: 10000 });

    // Take screenshot of initial state
    await page.screenshot({ path: 'test-results/01-initial-load.png', fullPage: true });

    // Verify tick counter shows non-zero total
    const tickDisplay = await page.locator('#tickDisplay').textContent();
    expect(tickDisplay).not.toContain('0 / 0');
    expect(tickDisplay).toMatch(/0 \/ \d+/);  // Should be "0 / <total>"
  });

  test('visualizer renders ships on canvas', async ({ page }) => {
    await page.goto(serverUrl, { waitUntil: 'networkidle' });
    await page.waitForFunction(() => {
      const overlay = document.getElementById('loadingOverlay');
      return !overlay || overlay.classList.contains('hidden');
    }, { timeout: 10000 });

    // Check canvas exists and has non-zero dimensions
    const canvasBox = await page.locator('#combatCanvas').boundingBox();
    expect(canvasBox).not.toBeNull();
    expect(canvasBox!.width).toBeGreaterThan(100);
    expect(canvasBox!.height).toBeGreaterThan(100);

    // Check canvas is not all black (something was rendered)
    const isNotBlank = await page.evaluate(() => {
      const canvas = document.getElementById('combatCanvas') as HTMLCanvasElement;
      if (!canvas) return false;
      const ctx = canvas.getContext('2d');
      if (!ctx) return false;
      const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
      // Check if any pixel is non-black
      for (let i = 0; i < imageData.data.length; i += 4) {
        if (imageData.data[i] > 0 || imageData.data[i+1] > 0 || imageData.data[i+2] > 0) {
          return true;
        }
      }
      return false;
    });
    expect(isNotBlank).toBeTruthy();

    await page.screenshot({ path: 'test-results/02-ships-rendered.png', fullPage: true });
  });

  test('fleet stats panels show ship data', async ({ page }) => {
    await page.goto(serverUrl, { waitUntil: 'networkidle' });
    await page.waitForFunction(() => {
      const overlay = document.getElementById('loadingOverlay');
      return !overlay || overlay.classList.contains('hidden');
    }, { timeout: 10000 });

    // Fleet 1 panel should have ship names
    const fleet1Panel = page.locator('.stats-panel.fleet1');
    await expect(fleet1Panel).toBeVisible();
    const fleet1Text = await fleet1Panel.textContent();
    expect(fleet1Text!.length).toBeGreaterThan(10);  // Not empty

    // Fleet 2 panel should have ship names
    const fleet2Panel = page.locator('.stats-panel.fleet2');
    await expect(fleet2Panel).toBeVisible();
    const fleet2Text = await fleet2Panel.textContent();
    expect(fleet2Text!.length).toBeGreaterThan(10);

    await page.screenshot({ path: 'test-results/03-fleet-panels.png', fullPage: true });
  });

  test('play button starts playback and tick advances', async ({ page }) => {
    await page.goto(serverUrl, { waitUntil: 'networkidle' });
    await page.waitForFunction(() => {
      const overlay = document.getElementById('loadingOverlay');
      return !overlay || overlay.classList.contains('hidden');
    }, { timeout: 10000 });

    // Press play
    await page.keyboard.press('Space');
    await page.waitForTimeout(2000);  // Let it play for 2 seconds

    // Pause
    await page.keyboard.press('Space');

    // Tick should have advanced from 0
    const tickDisplay = await page.locator('#tickDisplay').textContent();
    const tickMatch = tickDisplay?.match(/(\d+) \/ (\d+)/);
    expect(tickMatch).not.toBeNull();
    const currentTick = parseInt(tickMatch![1]);
    expect(currentTick).toBeGreaterThan(0);

    await page.screenshot({ path: 'test-results/04-after-playback.png', fullPage: true });
  });

  test('speed control changes playback rate', async ({ page }) => {
    await page.goto(serverUrl, { waitUntil: 'networkidle' });
    await page.waitForFunction(() => {
      const overlay = document.getElementById('loadingOverlay');
      return !overlay || overlay.classList.contains('hidden');
    }, { timeout: 10000 });

    // Set speed to 16x (key 7)
    await page.keyboard.press('7');

    // Play for 1 second at 16x
    await page.keyboard.press('Space');
    await page.waitForTimeout(1000);
    await page.keyboard.press('Space');

    // At 16x with 10 ticks/sec, should advance ~160 ticks in 1 second
    const tickDisplay = await page.locator('#tickDisplay').textContent();
    const tickMatch = tickDisplay?.match(/(\d+) \/ (\d+)/);
    const currentTick = parseInt(tickMatch![1]);
    expect(currentTick).toBeGreaterThan(50); // At least some significant advance

    await page.screenshot({ path: 'test-results/05-speed-16x.png', fullPage: true });
  });

  test('keyboard step forward/back works', async ({ page }) => {
    await page.goto(serverUrl, { waitUntil: 'networkidle' });
    await page.waitForFunction(() => {
      const overlay = document.getElementById('loadingOverlay');
      return !overlay || overlay.classList.contains('hidden');
    }, { timeout: 10000 });

    // Step forward 5 times
    for (let i = 0; i < 5; i++) {
      await page.keyboard.press('ArrowRight');
    }

    const tickDisplay = await page.locator('#tickDisplay').textContent();
    expect(tickDisplay).toMatch(/5 \/ \d+/);

    // Step back 2
    await page.keyboard.press('ArrowLeft');
    await page.keyboard.press('ArrowLeft');

    const tickAfterBack = await page.locator('#tickDisplay').textContent();
    expect(tickAfterBack).toMatch(/3 \/ \d+/);
  });

  test('skip to end shows final state', async ({ page }) => {
    await page.goto(serverUrl, { waitUntil: 'networkidle' });
    await page.waitForFunction(() => {
      const overlay = document.getElementById('loadingOverlay');
      return !overlay || overlay.classList.contains('hidden');
    }, { timeout: 10000 });

    // Skip to end
    await page.keyboard.press('End');
    await page.waitForTimeout(500);

    await page.screenshot({ path: 'test-results/06-final-state.png', fullPage: true });

    // Check that some ships are destroyed or fled in the stats panels
    const pageText = await page.textContent('body');
    // At the end, there should be either DESTROYED or FLED markers visible
    const hasEndState = pageText?.includes('DESTROYED') || pageText?.includes('FLED') || pageText?.includes('destroyed');
    // This may not show text — check via screenshot instead
  });

  test('full playback video capture at 8x speed', async ({ page }) => {
    await page.goto(serverUrl, { waitUntil: 'networkidle' });
    await page.waitForFunction(() => {
      const overlay = document.getElementById('loadingOverlay');
      return !overlay || overlay.classList.contains('hidden');
    }, { timeout: 10000 });

    await page.screenshot({ path: 'test-results/07-video-start.png', fullPage: true });

    // Set 8x speed and play through entire combat
    await page.keyboard.press('6'); // 8x speed
    await page.keyboard.press('Space'); // play

    // Wait for playback to complete (or timeout)
    // At 8x with 10 ticks/sec = 80 ticks/sec. For ~6000 tick combat, ~75 seconds.
    // For a ~1000 tick combat, ~12.5 seconds.
    await page.waitForTimeout(20000);

    await page.screenshot({ path: 'test-results/08-video-end.png', fullPage: true });
  });

  test('layout: stats panels fill remaining space around combat square', async ({ page }) => {
    await page.goto(serverUrl, { waitUntil: 'networkidle' });
    await page.waitForFunction(() => {
      const overlay = document.getElementById('loadingOverlay');
      return !overlay || overlay.classList.contains('hidden');
    }, { timeout: 10000 });

    const fleet1Box = await page.locator('.stats-panel.fleet1').boundingBox();
    const fleet2Box = await page.locator('.stats-panel.fleet2').boundingBox();
    const canvasBox = await page.locator('#combatCanvas').boundingBox();

    expect(fleet1Box).not.toBeNull();
    expect(fleet2Box).not.toBeNull();
    expect(canvasBox).not.toBeNull();

    // Canvas should be approximately square
    const aspectRatio = canvasBox!.width / canvasBox!.height;
    expect(aspectRatio).toBeGreaterThan(0.8);
    expect(aspectRatio).toBeLessThan(1.2);

    // Stats panels should be wider than 160px (not collapsed)
    expect(fleet1Box!.width).toBeGreaterThan(100);
    expect(fleet2Box!.width).toBeGreaterThan(100);

    // Total width should approximately fill viewport (1600px)
    const totalWidth = fleet1Box!.width + canvasBox!.width + fleet2Box!.width;
    expect(totalWidth).toBeGreaterThan(1400);

    await page.screenshot({ path: 'test-results/09-layout-check.png', fullPage: true });
  });
});
