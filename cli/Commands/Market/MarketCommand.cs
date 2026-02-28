using System.CommandLine;
using System.Text.Json;
using psecsapi.Console.Infrastructure.Configuration;
using psecsapi.Console.Infrastructure.Http;

namespace psecsapi.Console.Commands.Market
{
    public class MarketCommand : ICommand
    {
        private readonly AuthenticatedHttpClient _client;
        private readonly CliConfig _config;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public MarketCommand(AuthenticatedHttpClient client, CliConfig config)
        {
            _client = client;
            _config = config;
        }

        public Command Build()
        {
            var command = new Command("market", "Browse and manage Nexus Market sales");

            command.AddCommand(BuildListCommand());
            command.AddCommand(BuildShowCommand());
            command.AddCommand(BuildCreateCommand());
            command.AddCommand(BuildRepostCommand());
            command.AddCommand(BuildBuyCommand());
            command.AddCommand(BuildBidCommand());
            command.AddCommand(BuildCancelCommand());
            command.AddCommand(BuildRetrieveCommand());
            command.AddCommand(BuildMySalesCommand());
            command.AddCommand(BuildMyBidsCommand());

            return command;
        }

        #region List Command

        private Command BuildListCommand()
        {
            var typeOption = new Option<string?>("--type", "Filter by sale type (buynow, auction)");
            typeOption.AddAlias("-t");

            var assetTypeOption = new Option<string?>("--asset-type", "Filter by asset type");

            var sellerOption = new Option<Guid?>("--seller", "Filter by seller corp ID");

            var minPriceOption = new Option<long?>("--min-price", "Minimum price");
            var maxPriceOption = new Option<long?>("--max-price", "Maximum price");

            var endingSoonOption = new Option<string?>("--ending-soon", "Filter by time remaining (e.g., 1d, 6h)");

            var sortOption = new Option<string?>("--sort", "Sort by: price, time, newest, bids");
            var descOption = new Option<bool>("--desc", () => true, "Sort descending");
            var ascOption = new Option<bool>("--asc", () => false, "Sort ascending");

            var pageOption = new Option<int>("--page", () => 1, "Page number");
            var limitOption = new Option<int>("--limit", () => 20, "Results per page (max 100)");

            var jsonOption = new Option<bool>("--json", () => false, "Output as JSON");

            var command = new Command("list", "Browse active market listings")
            {
                typeOption, assetTypeOption, sellerOption,
                minPriceOption, maxPriceOption, endingSoonOption,
                sortOption, descOption, ascOption,
                pageOption, limitOption, jsonOption
            };

            command.SetHandler(async (context) =>
            {
                var type = context.ParseResult.GetValueForOption(typeOption);
                var assetType = context.ParseResult.GetValueForOption(assetTypeOption);
                var seller = context.ParseResult.GetValueForOption(sellerOption);
                var minPrice = context.ParseResult.GetValueForOption(minPriceOption);
                var maxPrice = context.ParseResult.GetValueForOption(maxPriceOption);
                var endingSoon = context.ParseResult.GetValueForOption(endingSoonOption);
                var sort = context.ParseResult.GetValueForOption(sortOption);
                var desc = context.ParseResult.GetValueForOption(descOption);
                var asc = context.ParseResult.GetValueForOption(ascOption);
                var page = context.ParseResult.GetValueForOption(pageOption);
                var limit = context.ParseResult.GetValueForOption(limitOption);
                var json = context.ParseResult.GetValueForOption(jsonOption);

                var queryParams = new List<string>();
                if (!string.IsNullOrEmpty(type)) queryParams.Add($"type={type}");
                if (!string.IsNullOrEmpty(assetType)) queryParams.Add($"assetType={assetType}");
                if (seller.HasValue) queryParams.Add($"seller={seller}");
                if (minPrice.HasValue) queryParams.Add($"minPrice={minPrice}");
                if (maxPrice.HasValue) queryParams.Add($"maxPrice={maxPrice}");
                if (!string.IsNullOrEmpty(endingSoon)) queryParams.Add($"endingSoon={endingSoon}");
                if (!string.IsNullOrEmpty(sort)) queryParams.Add($"sort={sort}");
                queryParams.Add($"desc={!asc && desc}");
                queryParams.Add($"page={page}");
                queryParams.Add($"limit={Math.Min(limit, 100)}");

                var query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                var response = await _client.GetAsync($"/api/market{query}");
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                if (json)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                var listings = JsonSerializer.Deserialize<MarketListingResponse>(content, JsonOptions);
                if (listings == null || listings.Listings.Count == 0)
                {
                    System.Console.WriteLine("No listings found.");
                    return;
                }

                System.Console.WriteLine();
                System.Console.WriteLine($"{"ID",-38} {"Type",-8} {"Asset",-25} {"Price",12} {"Ends",-12} {"Bids",5}  {"Afford",-6}");
                System.Console.WriteLine(new string('-', 114));

                foreach (var listing in listings.Listings)
                {
                    var asset = listing.AssetSummary.Length > 24
                        ? listing.AssetSummary[..21] + "..."
                        : listing.AssetSummary;
                    var typeStr = listing.Type == "Auction" ? "Auction" : "BuyNow";
                    var afford = listing.CanAfford
                        ? "Yes"
                        : $"No (-{listing.InsufficientFundsAmount:N0})";

                    System.Console.WriteLine($"{listing.SaleId,-38} {typeStr,-8} {asset,-25} {listing.Price,12:N0} {listing.TimeRemaining,-12} {listing.BidCount,5}  {afford}");
                }

                System.Console.WriteLine();
                System.Console.WriteLine($"Page {listings.Page} of {listings.TotalPages} ({listings.TotalItems} total)");
            });

            return command;
        }

        #endregion

        #region Show Command

        private Command BuildShowCommand()
        {
            var saleIdArg = new Argument<Guid>("sale-id", "The sale ID to view");
            var jsonOption = new Option<bool>("--json", () => false, "Output as JSON");

            var command = new Command("show", "View sale details") { saleIdArg, jsonOption };

            command.SetHandler(async (saleId, json) =>
            {
                var response = await _client.GetAsync($"/api/market/{saleId}");
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                if (json)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                var sale = JsonSerializer.Deserialize<SaleDetailsResponse>(content, JsonOptions);
                if (sale == null)
                {
                    System.Console.WriteLine("Error parsing response");
                    return;
                }

                System.Console.WriteLine();
                System.Console.WriteLine($"=== Sale Details ===");
                System.Console.WriteLine();
                System.Console.WriteLine($"Sale ID: {sale.SaleId}");
                System.Console.WriteLine($"Type: {sale.Type}");
                System.Console.WriteLine($"State: {sale.State}");
                System.Console.WriteLine();
                System.Console.WriteLine($"Asset: {sale.AssetSummary}");
                System.Console.WriteLine($"Asset ID: {sale.BoxedAssetId}");
                System.Console.WriteLine();
                System.Console.WriteLine($"Seller: {sale.SellerCorpName}");
                if (!string.IsNullOrEmpty(sale.Description))
                    System.Console.WriteLine($"Description: {sale.Description}");
                System.Console.WriteLine();
                System.Console.WriteLine("Pricing:");
                if (sale.Type == "Auction")
                {
                    System.Console.WriteLine($"  Starting Price: {sale.StartingPrice:N0} credits");
                    System.Console.WriteLine($"  Current Bid: {sale.Price:N0} credits ({sale.BidCount} bids)");
                    System.Console.WriteLine($"  Minimum Next Bid: {sale.MinimumNextBid:N0} credits");
                }
                else
                {
                    System.Console.WriteLine($"  Price: {sale.Price:N0} credits");
                }
                System.Console.WriteLine();
                System.Console.WriteLine("Timing:");
                System.Console.WriteLine($"  Listed: {sale.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");
                System.Console.WriteLine($"  Expires: {sale.ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC ({sale.TimeRemaining} remaining)");
                System.Console.WriteLine($"  Pickup Window Ends: {sale.PickupWindowEndsAt:yyyy-MM-dd HH:mm:ss} UTC");

            }, saleIdArg, jsonOption);

            return command;
        }

        #endregion

        #region Create Command

        private Command BuildCreateCommand()
        {
            var shipOption = new Option<Guid>("--ship", "Ship ID containing the asset") { IsRequired = true };
            shipOption.AddAlias("-s");

            var assetOption = new Option<Guid>("--asset", "Boxed asset ID to sell") { IsRequired = true };
            assetOption.AddAlias("-a");

            var priceOption = new Option<long>("--price", "Sale price (or starting price for auctions)") { IsRequired = true };
            priceOption.AddAlias("-p");

            var durationOption = new Option<int>("--duration", "Duration in days (1-10)") { IsRequired = true };

            var auctionOption = new Option<bool>("--auction", () => false, "Create as auction instead of Buy Now");

            var descriptionOption = new Option<string?>("--description", "Sale description (max 200 chars)");
            descriptionOption.AddAlias("-d");

            var jsonOption = new Option<bool>("--json", () => false, "Output as JSON");

            var command = new Command("create", "Create a new market sale")
            {
                shipOption, assetOption, priceOption, durationOption,
                auctionOption, descriptionOption, jsonOption
            };

            command.SetHandler(async (ship, asset, price, duration, auction, description, json) =>
            {
                var request = new
                {
                    ShipId = ship,
                    BoxedAssetId = asset,
                    Price = price,
                    DurationDays = duration,
                    IsAuction = auction,
                    Description = description ?? ""
                };

                var response = await _client.PostAsync("/api/market", request);
                var content = await response.Content.ReadAsStringAsync();

                if (json)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                var result = JsonSerializer.Deserialize<SaleResultResponse>(content, JsonOptions);
                if (result == null)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                if (!result.Success)
                {
                    System.Console.WriteLine($"Error: {result.ErrorMessage} ({result.ErrorCode})");
                    return;
                }

                var typeStr = auction ? "Auction" : "Buy Now";
                System.Console.WriteLine();
                System.Console.WriteLine($"Created {typeStr} sale!");
                System.Console.WriteLine($"Sale ID: {result.SaleId}");
                if (result.FeesCharged > 0)
                    System.Console.WriteLine($"Storage Fee: {result.FeesCharged:N0} credits");
                System.Console.WriteLine($"Expires: {result.ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC");
                System.Console.WriteLine($"Pickup Window Ends: {result.PickupWindowEndsAt:yyyy-MM-dd HH:mm:ss} UTC");

            }, shipOption, assetOption, priceOption, durationOption, auctionOption, descriptionOption, jsonOption);

            return command;
        }

        #endregion

        #region Repost Command

        private Command BuildRepostCommand()
        {
            var saleIdArg = new Argument<Guid>("sale-id", "The sale ID to repost");

            var priceOption = new Option<long>("--price", "New sale price (or starting price for auctions)") { IsRequired = true };
            priceOption.AddAlias("-p");

            var durationOption = new Option<int>("--duration", "Duration in days (1-10)") { IsRequired = true };

            var auctionOption = new Option<bool?>("--auction", "Change to auction (true) or buy now (false)");

            var descriptionOption = new Option<string?>("--description", "New sale description (max 200 chars)");
            descriptionOption.AddAlias("-d");

            var jsonOption = new Option<bool>("--json", () => false, "Output as JSON");

            var command = new Command("repost", "Repost an expired or unsold sale")
            {
                saleIdArg, priceOption, durationOption,
                auctionOption, descriptionOption, jsonOption
            };

            command.SetHandler(async (saleId, price, duration, auction, description, json) =>
            {
                var request = new
                {
                    Price = price,
                    DurationDays = duration,
                    IsAuction = auction,
                    Description = description
                };

                var response = await _client.PostAsync($"/api/market/{saleId}/repost", request);
                var content = await response.Content.ReadAsStringAsync();

                if (json)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                var result = JsonSerializer.Deserialize<SaleResultResponse>(content, JsonOptions);
                if (result == null)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                if (!result.Success)
                {
                    System.Console.WriteLine($"Error: {result.ErrorMessage} ({result.ErrorCode})");
                    return;
                }

                System.Console.WriteLine();
                System.Console.WriteLine($"Sale reposted!");
                System.Console.WriteLine($"Sale ID: {result.SaleId}");
                if (result.FeesCharged > 0)
                    System.Console.WriteLine($"Storage Fee: {result.FeesCharged:N0} credits");
                System.Console.WriteLine($"Expires: {result.ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC");
                System.Console.WriteLine($"Pickup Window Ends: {result.PickupWindowEndsAt:yyyy-MM-dd HH:mm:ss} UTC");

            }, saleIdArg, priceOption, durationOption, auctionOption, descriptionOption, jsonOption);

            return command;
        }

        #endregion

        #region Buy Command

        private Command BuildBuyCommand()
        {
            var saleIdArg = new Argument<Guid>("sale-id", "The sale ID to buy");
            var jsonOption = new Option<bool>("--json", () => false, "Output as JSON");

            var command = new Command("buy", "Buy a listing immediately") { saleIdArg, jsonOption };

            command.SetHandler(async (saleId, json) =>
            {
                var response = await _client.PostAsync($"/api/market/{saleId}/purchase", new { });
                var content = await response.Content.ReadAsStringAsync();

                if (json)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                var result = JsonSerializer.Deserialize<SaleResultResponse>(content, JsonOptions);
                if (result == null)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                if (!result.Success)
                {
                    System.Console.WriteLine($"Error: {result.ErrorMessage} ({result.ErrorCode})");
                    return;
                }

                System.Console.WriteLine();
                System.Console.WriteLine($"Purchase successful!");
                System.Console.WriteLine($"Sale ID: {result.SaleId}");
                System.Console.WriteLine($"New State: {result.NewState}");
                System.Console.WriteLine();
                System.Console.WriteLine($"Use 'market retrieve {result.SaleId} --ship <ship-id> --cargo <cargo-module-id>' to pick up your purchase.");
                System.Console.WriteLine($"Pickup window ends: {result.PickupWindowEndsAt:yyyy-MM-dd HH:mm:ss} UTC");

            }, saleIdArg, jsonOption);

            return command;
        }

        #endregion

        #region Bid Command

        private Command BuildBidCommand()
        {
            var saleIdArg = new Argument<Guid>("sale-id", "The auction sale ID to bid on");
            var amountArg = new Argument<long>("amount", "The bid amount in credits");
            var jsonOption = new Option<bool>("--json", () => false, "Output as JSON");

            var command = new Command("bid", "Place a bid on an auction") { saleIdArg, amountArg, jsonOption };

            command.SetHandler(async (saleId, amount, json) =>
            {
                var request = new { Amount = amount };

                var response = await _client.PostAsync($"/api/market/{saleId}/bid", request);
                var content = await response.Content.ReadAsStringAsync();

                if (json)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                var result = JsonSerializer.Deserialize<SaleResultResponse>(content, JsonOptions);
                if (result == null)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                if (!result.Success)
                {
                    System.Console.WriteLine($"Error: {result.ErrorMessage} ({result.ErrorCode})");
                    return;
                }

                System.Console.WriteLine();
                System.Console.WriteLine($"Bid placed successfully!");
                System.Console.WriteLine($"Your bid: {amount:N0} credits");
                System.Console.WriteLine($"You are currently the high bidder.");
                System.Console.WriteLine($"Use 'market show {result.SaleId}' to see current auction status.");

            }, saleIdArg, amountArg, jsonOption);

            return command;
        }

        #endregion

        #region Cancel Command

        private Command BuildCancelCommand()
        {
            var saleIdArg = new Argument<Guid>("sale-id", "The sale ID to cancel");
            var jsonOption = new Option<bool>("--json", () => false, "Output as JSON");

            var command = new Command("cancel", "Cancel your active sale") { saleIdArg, jsonOption };

            command.SetHandler(async (saleId, json) =>
            {
                var response = await _client.PostAsync($"/api/market/{saleId}/cancel", new { });
                var content = await response.Content.ReadAsStringAsync();

                if (json)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                var result = JsonSerializer.Deserialize<SaleResultResponse>(content, JsonOptions);
                if (result == null)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                if (!result.Success)
                {
                    System.Console.WriteLine($"Error: {result.ErrorMessage} ({result.ErrorCode})");
                    return;
                }

                System.Console.WriteLine();
                System.Console.WriteLine($"Sale cancelled!");
                System.Console.WriteLine($"Sale ID: {result.SaleId}");
                System.Console.WriteLine($"New State: {result.NewState}");
                System.Console.WriteLine();
                System.Console.WriteLine($"Use 'market retrieve {result.SaleId} --ship <ship-id> --cargo <cargo-module-id>' to retrieve your asset.");
                System.Console.WriteLine($"Pickup window ends: {result.PickupWindowEndsAt:yyyy-MM-dd HH:mm:ss} UTC");

            }, saleIdArg, jsonOption);

            return command;
        }

        #endregion

        #region Retrieve Command

        private Command BuildRetrieveCommand()
        {
            var saleIdArg = new Argument<Guid>("sale-id", "The sale ID to retrieve asset from");

            var shipOption = new Option<Guid>("--ship", "Ship ID to receive the asset") { IsRequired = true };
            shipOption.AddAlias("-s");

            var cargoOption = new Option<Guid>("--cargo", "Cargo module ID on the destination ship") { IsRequired = true };
            cargoOption.AddAlias("-c");

            var jsonOption = new Option<bool>("--json", () => false, "Output as JSON");

            var command = new Command("retrieve", "Retrieve asset from a completed sale")
            {
                saleIdArg, shipOption, cargoOption, jsonOption
            };

            command.SetHandler(async (saleId, ship, cargo, json) =>
            {
                var request = new { ShipId = ship, CargoModuleId = cargo };

                var response = await _client.PostAsync($"/api/market/{saleId}/retrieve", request);
                var content = await response.Content.ReadAsStringAsync();

                if (json)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                var result = JsonSerializer.Deserialize<SaleResultResponse>(content, JsonOptions);
                if (result == null)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                if (!result.Success)
                {
                    System.Console.WriteLine($"Error: {result.ErrorMessage} ({result.ErrorCode})");
                    return;
                }

                System.Console.WriteLine();
                System.Console.WriteLine($"Asset retrieved successfully!");
                System.Console.WriteLine($"Sale ID: {result.SaleId}");
                System.Console.WriteLine($"New State: {result.NewState}");
                System.Console.WriteLine();
                System.Console.WriteLine($"The asset has been transferred to ship {ship}.");

            }, saleIdArg, shipOption, cargoOption, jsonOption);

            return command;
        }

        #endregion

        #region My Sales Command

        private Command BuildMySalesCommand()
        {
            var stateOption = new Option<string?>("--state", "Filter by state (active, expired, sold, cancelled)");
            stateOption.AddAlias("-s");

            var pageOption = new Option<int>("--page", () => 1, "Page number");
            var limitOption = new Option<int>("--limit", () => 20, "Results per page (max 100)");
            var jsonOption = new Option<bool>("--json", () => false, "Output as JSON");

            var command = new Command("my-sales", "View your sales")
            {
                stateOption, pageOption, limitOption, jsonOption
            };

            command.SetHandler(async (state, page, limit, json) =>
            {
                var corpId = _config.User.DefaultCorpId;
                if (!corpId.HasValue)
                {
                    System.Console.WriteLine("Error: No default corporation set. Use 'config set-corp <corp-id>' first.");
                    return;
                }

                var queryParams = new List<string>();
                if (!string.IsNullOrEmpty(state)) queryParams.Add($"state={state}");
                queryParams.Add($"page={page}");
                queryParams.Add($"limit={Math.Min(limit, 100)}");

                var query = "?" + string.Join("&", queryParams);
                var response = await _client.GetAsync($"/api/market/my-sales{query}");
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                if (json)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                var result = JsonSerializer.Deserialize<MySalesResponse>(content, JsonOptions);
                if (result == null || result.Listings.Count == 0)
                {
                    System.Console.WriteLine("No sales found.");
                    return;
                }

                System.Console.WriteLine();
                System.Console.WriteLine($"{"ID",-38} {"State",-10} {"Type",-8} {"Asset",-20} {"Price",12} {"Ends",-12}");
                System.Console.WriteLine(new string('-', 104));

                foreach (var sale in result.Listings)
                {
                    var asset = sale.AssetSummary.Length > 19
                        ? sale.AssetSummary[..16] + "..."
                        : sale.AssetSummary;
                    var typeStr = sale.Type == "Auction" ? "Auction" : "BuyNow";

                    System.Console.WriteLine($"{sale.SaleId,-38} {sale.State,-10} {typeStr,-8} {asset,-20} {sale.Price,12:N0} {sale.TimeRemaining,-12}");
                }

                System.Console.WriteLine();
                System.Console.WriteLine($"Page {result.Page} of {result.TotalPages} ({result.TotalItems} total)");

            }, stateOption, pageOption, limitOption, jsonOption);

            return command;
        }

        #endregion

        #region My Bids Command

        private Command BuildMyBidsCommand()
        {
            var jsonOption = new Option<bool>("--json", () => false, "Output as JSON");

            var command = new Command("my-bids", "View your bids")
            {
                jsonOption
            };

            command.SetHandler(async (json) =>
            {
                var corpId = _config.User.DefaultCorpId;
                if (!corpId.HasValue)
                {
                    System.Console.WriteLine("Error: No default corporation set. Use 'config set-corp <corp-id>' first.");
                    return;
                }

                var response = await _client.GetAsync("/api/market/my-bids");
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                if (json)
                {
                    System.Console.WriteLine(content);
                    return;
                }

                var bids = JsonSerializer.Deserialize<List<MyBidItem>>(content, JsonOptions);
                if (bids == null || bids.Count == 0)
                {
                    System.Console.WriteLine("No bids found.");
                    return;
                }

                System.Console.WriteLine();
                System.Console.WriteLine($"{"Sale ID",-38} {"Status",-10} {"Asset",-20} {"Your Bid",12} {"Current",12} {"Ends",-12}");
                System.Console.WriteLine(new string('-', 108));

                foreach (var bid in bids)
                {
                    var asset = bid.AssetSummary.Length > 19
                        ? bid.AssetSummary[..16] + "..."
                        : bid.AssetSummary;

                    System.Console.WriteLine($"{bid.SaleId,-38} {bid.BidStatus,-10} {asset,-20} {bid.YourBidAmount,12:N0} {bid.CurrentHighBid,12:N0} {bid.TimeRemaining,-12}");
                }

            }, jsonOption);

            return command;
        }

        #endregion

        #region Response Models

        private class MarketListingResponse
        {
            public List<MarketListingItem> Listings { get; set; } = new();
            public int Page { get; set; }
            public int PageSize { get; set; }
            public int TotalItems { get; set; }
            public int TotalPages { get; set; }
        }

        private class MarketListingItem
        {
            public Guid SaleId { get; set; }
            public string Type { get; set; } = string.Empty;
            public string SellerCorpName { get; set; } = string.Empty;
            public string AssetSummary { get; set; } = string.Empty;
            public long Price { get; set; }
            public long? StartingPrice { get; set; }
            public int BidCount { get; set; }
            public string TimeRemaining { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public bool CanAfford { get; set; }
            public long? InsufficientFundsAmount { get; set; }
        }

        private class SaleDetailsResponse
        {
            public Guid SaleId { get; set; }
            public string Type { get; set; } = string.Empty;
            public string State { get; set; } = string.Empty;
            public string SellerCorpName { get; set; } = string.Empty;
            public Guid BoxedAssetId { get; set; }
            public string AssetSummary { get; set; } = string.Empty;
            public long Price { get; set; }
            public long? StartingPrice { get; set; }
            public int BidCount { get; set; }
            public long MinimumNextBid { get; set; }
            public string Description { get; set; } = string.Empty;
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset ExpiresAt { get; set; }
            public DateTimeOffset PickupWindowEndsAt { get; set; }
            public string TimeRemaining { get; set; } = string.Empty;
        }

        private class SaleResultResponse
        {
            public bool Success { get; set; }
            public string? ErrorCode { get; set; }
            public string? ErrorMessage { get; set; }
            public Guid? SaleId { get; set; }
            public string? NewState { get; set; }
            public long? FeesCharged { get; set; }
            public DateTimeOffset? ExpiresAt { get; set; }
            public DateTimeOffset? PickupWindowEndsAt { get; set; }
        }

        private class MySalesResponse
        {
            public List<MySaleItem> Listings { get; set; } = new();
            public int Page { get; set; }
            public int PageSize { get; set; }
            public int TotalItems { get; set; }
            public int TotalPages { get; set; }
        }

        private class MySaleItem
        {
            public Guid SaleId { get; set; }
            public string Type { get; set; } = string.Empty;
            public string State { get; set; } = string.Empty;
            public string AssetSummary { get; set; } = string.Empty;
            public long Price { get; set; }
            public int BidCount { get; set; }
            public string TimeRemaining { get; set; } = string.Empty;
        }

        private class MyBidItem
        {
            public Guid SaleId { get; set; }
            public string Type { get; set; } = string.Empty;
            public string SellerCorpName { get; set; } = string.Empty;
            public string AssetSummary { get; set; } = string.Empty;
            public long CurrentHighBid { get; set; }
            public int BidCount { get; set; }
            public DateTimeOffset ExpiresAt { get; set; }
            public string TimeRemaining { get; set; } = string.Empty;
            public long YourBidAmount { get; set; }
            public string BidStatus { get; set; } = string.Empty;
        }

        #endregion
    }
}
