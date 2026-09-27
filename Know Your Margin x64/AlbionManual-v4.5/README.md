# Materials improvements in 4.5

Material type offers All materials, Raw Resources, Refined Resources and Other Ingredients. Raw means wood/logs, ore, hide, fiber and rock; refined means planks, bars, leather, cloth and blocks. Herbs, milk and other crafting inputs remain in Other Ingredients. These filters combine with city, tier, enchantment and search. Missing prices only shows unpriced items in the selected server and city. After Save prices the list refreshes to remove newly priced items when this filter is active.

The material-name column is frozen during horizontal scrolling. Last updated records price changes from either price editor. Older prices show Unknown (older price); no dates are invented. Missing prices show Not priced. Price dates are stored beside the price file in its .dates.json companion; include it in backups. Dates never expire or change prices automatically.

Materials-window city, tier, enchantment, group, search and missing-price filter are remembered in config/materials-view.json. The server continues to come from the main window. Invalid price drafts prevent switching filters or closing, so they can be corrected without loss.

63 core checks passed, including group classification, price-date persistence and legacy prices. Published UI checks covered missing-price filtering, saved filters, price editing and refreshing. The Materials window was rendered and visually reviewed.

## Materials window and enchantment sorting

Select Materials at the top to open the price editor. It inherits the current server; select any city, tier, and enchantment (.0 is normal). All tiers includes ingredients without a tier. The catalog contains unique ingredients from the installed recipes, excluding crafting artifacts. Other recipe inputs remain available. Edit the price column and select Save prices. Valid drafts also save when filters change or the window closes; invalid drafts prevent that action and show an error. Blank values clear prices. Only edited rows are saved. Matching main-window prices and totals refresh immediately; existing session snapshots stay unchanged.

The main recipe picker also has an enchantment filter and sorts by tier, then enchantment, then name. All filters are available for broad browsing. 57 core checks and published-window checks passed, including T5.3 plank filtering, artifact exclusion, saved price reload and refresh notification.

# Albion Manual Crafting 4.5

Run app/AlbionScanner.exe. If using the ZIP, extract the entire archive first and keep its files together. The app includes the Windows x64 runtime and operates offline. Current local saved prices and sessions were copied into this release; back up app/config before replacing or moving it.

## Material total

Saved material prices now shows a Material Total beneath the Extended cost column. It sums the material costs for the selected starting batch, excluding crafting and market fees. Missing prices or invalid inputs show a dash. Saved sessions retain their separate inventory and costs.

## City production bonus strip

Below the server/city/category/tier/search controls, the selected city's fixed specialties appear horizontally. Refining comes first (Wood, Fiber, Stone, Hide or Ore at +40%), followed by butchering at +10% where applicable and crafting at +15%. Categories sharing a percentage are grouped. Text wraps on smaller windows. Caerleon and Brecilien have no refining specialty listed.

These are informational production bonuses, not resource return percentages. Daily bonuses and the general city base bonus are excluded. No prices, returns, profits or session data are changed by the strip. It follows the material-city selector and is independent of the selected saved session's city.

Mappings were reviewed with the user using the Albion Wiki resource-return table, city crafting references and the Fort Sterling in-game screenshot. See https://wiki.albiononline.com/wiki/Resource_return_rate and https://www.albioncodex.com/guides/best-city-to-craft-albion-online . Text is maintained in source/Scanner.Desktop/CityBonuses.cs.

All seven city selections were checked against the published application, and the Fort Sterling layout was rendered and visually inspected.

## Streamlined workflow

1. Choose a recipe and saved material prices. Item names are displayed; identifiers remain internal.
2. Open New session setup, set your starting batch and initial saved sell price, then select Start new session. Starting stock is copied into the session and setup collapses. Existing sessions can be selected without using setup again.
3. The selected session has one expected sell price. Changes save when you leave the field; saving a round also validates and saves it. This changes the selected session's price, not other sessions or the global saved price.
4. Enter crafts completed this round, the additional crafting fee paid for this round, and exact material quantities you have after the round. Select Save round. Fees accumulate automatically; quantities REPLACE earlier inventory. The fee field resets to zero after saving.
5. Use Add purchased materials only for extra purchases. Purchased quantities and costs are added to the session.
6. Read the three profit cards below the round controls. The combined material table shows available inventory, consumption per craft, return input, saved unit price and remaining value. Values reflect the last saved round, not draft inputs.

Undo last saved action removes the last round and its fee or the last purchase. Earlier session-wide fees from version 4.0 are preserved once, shown in the cost explanation, and not assigned to individual rounds. Do not enter those fees again. Recipe silver is still charged per craft; round fees should only include additional charges.

## Market fee assumptions

Results separately show a 2.5% setup fee and 4% Premium sales tax, both applied to gross expected sales. Combined, these remain 6.5% for one sell order that fully sells at its listed price. Setup fees are paid on listing; sales tax is paid on sale. The estimate assumes Premium sell orders, not instant sales or non-Premium tax. Relisting charges and the game's per-transaction rounding are not modeled.

Expected sales profit = gross sales - setup fee - sales tax - all session input and crafting costs.
Remaining material value = latest exact inventory quantities × session saved unit prices.
Combined profit = expected sales profit + remaining material value.
ROI excluding leftovers = expected sales profit / all session costs × 100.
ROI including leftovers = combined profit / (all session costs - remaining value) × 100.
ROI is undefined when the denominator is not positive. Remaining materials represent inventory value, not cash received.

## Saved data

All editable configuration is in app/config. recipes.json holds recipes; manual-prices.json and sell-prices.json hold personal prices; crafting-sessions.json holds sessions including per-round fees. Prices never scan or update automatically. Session recipes and material valuation prices are frozen on creation. Switching sessions discards unsaved round drafts. Back up the entire config folder.

Recipe editing uses material names selected from the catalog. Existing item identifiers remain unchanged. New custom output items receive internal identifiers automatically. Advanced catalog additions can be made in recipes.json.

## Build and validation

Complete source is included. Install .NET 10 SDK on Windows and run source/Build.ps1 to test and publish a self-contained Windows x64 folder under build. Keep config beside the resulting executable. Do not publish into the bundle root containing source.

53 core checks cover calculations, inventories, prices, persistence, legacy fees, per-round fees, undo and the separate market fee amounts. Additional published-assembly checks construct the main window and exercise session round saving, fee totals, inventory replacement, sell-price persistence and draft preservation. The interface was rendered and visually inspected; a full visible executable launch was not performed. Startup diagnostics are written to app/startup.log.




