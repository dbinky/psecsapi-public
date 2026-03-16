import { test, expect } from '@playwright/test';
import { execSync, spawn, ChildProcess } from 'child_process';
import * as path from 'path';
import * as fs from 'fs';

const REPO_ROOT = path.resolve(__dirname, '..', '..', '..');
const CONSOLE_PROJECT = path.join(REPO_ROOT, 'psecsapi.Console');
const REPLAY_FILE = path.join(__dirname, 'test-replay.bin');

let serverProcess: ChildProcess | null = null;
let serverUrl: string = '';

function generateReplayIfNeeded(): void {
  if (fs.existsSync(REPLAY_FILE)) return;
  console.log('Generating test replay...');
  execSync(
    `dotnet run --project "${CONSOLE_PROJECT}" -- combat simulate ` +
    `--fleet1 preset:balanced-trio --fleet2 preset:heavy-assault ` +
    `--terrain rubble --seed 42 --output "${REPLAY_FILE}"`,
    { cwd: REPO_ROOT, timeout: 120000 }
  );
}

function startServer(): Promise<string> {
  return new Promise((resolve, reject) => {
    const proc = spawn('dotnet', [
      'run', '--no-build', '--project', CONSOLE_PROJECT,
      '--', 'combat', 'visualize', REPLAY_FILE, '--no-browser', '--port', '19999'
    ], { cwd: REPO_ROOT, stdio: ['pipe', 'pipe', 'pipe'] });

    serverProcess = proc;
    let stderr = '';

    proc.stderr?.on('data', (data: Buffer) => {
      stderr += data.toString();
      if (stderr.includes('localhost:19999')) {
        resolve('http://localhost:19999');
      }
    });

    proc.on('error', reject);
    setTimeout(() => {
      if (!serverUrl) reject(new Error('Server timeout. stderr: ' + stderr));
    }, 30000);
  });
}

test.describe('Layout Debug', () => {
  test.beforeAll(async () => {
    generateReplayIfNeeded();
    serverUrl = await startServer();
    console.log('Server at:', serverUrl);
    await new Promise(r => setTimeout(r, 1000));
  });

  test.afterAll(async () => {
    if (serverProcess) { serverProcess.kill('SIGTERM'); serverProcess = null; }
  });

  test('capture layout screenshot', async ({ page }) => {
    // Simulate maximized browser on 1920x1080 monitor (minus OS chrome)
    await page.setViewportSize({ width: 1920, height: 1040 });
    await page.goto(serverUrl, { waitUntil: 'networkidle' });
    await page.waitForFunction(() => {
      const overlay = document.getElementById('loadingOverlay');
      return !overlay || overlay.classList.contains('hidden');
    }, { timeout: 15000 });

    // Step forward a few ticks so ships are visible
    for (let i = 0; i < 10; i++) await page.keyboard.press('ArrowRight');
    await page.waitForTimeout(300);

    await page.screenshot({ path: path.join(__dirname, 'layout-debug.png'), fullPage: true });

    // Measure layout
    const measurements = await page.evaluate(() => {
      const mainArea = document.querySelector('.main-area') as HTMLElement;
      const fleet1 = document.querySelector('.stats-panel.fleet1') as HTMLElement;
      const fleet2 = document.querySelector('.stats-panel.fleet2') as HTMLElement;
      const combatContainer = document.querySelector('.combat-container') as HTMLElement;
      const canvas = document.getElementById('combatCanvas') as HTMLCanvasElement;
      const header = document.querySelector('.header-bar') as HTMLElement;
      const transport = document.querySelector('.transport-bar') as HTMLElement;

      return {
        viewport: { w: window.innerWidth, h: window.innerHeight },
        mainArea: mainArea ? { x: mainArea.offsetLeft, w: mainArea.offsetWidth, h: mainArea.offsetHeight } : null,
        fleet1: fleet1 ? { x: fleet1.offsetLeft, w: fleet1.offsetWidth, h: fleet1.offsetHeight } : null,
        fleet2: fleet2 ? { x: fleet2.offsetLeft, w: fleet2.offsetWidth, h: fleet2.offsetHeight } : null,
        combatContainer: combatContainer ? { x: combatContainer.offsetLeft, w: combatContainer.offsetWidth, h: combatContainer.offsetHeight } : null,
        canvas: canvas ? { x: canvas.offsetLeft, w: canvas.width, h: canvas.height, clientW: canvas.clientWidth, clientH: canvas.clientHeight } : null,
        header: header ? { h: header.offsetHeight } : null,
        transport: transport ? { h: transport.offsetHeight } : null,
      };
    });

    console.log('\n=== LAYOUT MEASUREMENTS ===');
    console.log(JSON.stringify(measurements, null, 2));

    // Verify canvas is roughly centered
    if (measurements.fleet1 && measurements.canvas && measurements.fleet2) {
      const fleet1Right = measurements.fleet1.x + measurements.fleet1.w;
      const canvasLeft = measurements.combatContainer!.x;
      const canvasRight = measurements.combatContainer!.x + measurements.combatContainer!.w;
      const fleet2Left = measurements.fleet2.x;
      const leftGap = canvasLeft - fleet1Right;
      const rightGap = fleet2Left - canvasRight;
      console.log(`\nLeft gap: ${leftGap}px, Right gap: ${rightGap}px, Difference: ${Math.abs(leftGap - rightGap)}px`);
      console.log(`Fleet1 width: ${measurements.fleet1.w}px, Fleet2 width: ${measurements.fleet2.w}px`);
      console.log(`Canvas: ${measurements.canvas.w}x${measurements.canvas.h}`);
      console.log(`Viewport: ${measurements.viewport.w}x${measurements.viewport.h}`);
    }
  });
});
