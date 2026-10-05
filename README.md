# Orders report

## How to run

Requires the .NET 10 SDK. Run the commands from the repository root; they are the same on Windows, macOS and Linux.

| Command | What it does |
|---|---|
| `dotnet run --project src/Orders` | shows all orders, then the statistics |
| `dotnet run --project src/Orders -- list` | shows all orders |
| `dotnet run --project src/Orders -- search Nino` | shows the orders of one customer |
| `dotnet run --project src/Orders -- stats` | shows the statistics for completed orders |
| `dotnet run --project src/Orders -- --file other.json stats` | uses another orders file |
| `dotnet test` | runs the tests |

Exit codes: `0` success, `1` the file is missing or its data is invalid, `2` wrong command line.

## AI tools used

_Written at the end, from what actually happened._

## What AI helped with

_Written at the end, from what actually happened._

## Errors or wrong logic found in AI-suggested code

_Written at the end, from what actually happened._

## How I verified and fixed the results

Before writing any code, I worked out the expected results by hand:

| Order | Customer | Status | Lines | Order total | Counted? |
|---|---|---|---|---|---|
| 1 | Nino | completed | Keyboard 2 × 50 + Mouse 1 × 25 | 125 | yes |
| 2 | Giorgi | cancelled | Keyboard 1 × 50 | 50 | **no** |
| 3 | Nino | completed | Mouse 2 × 25 + Monitor 1 × 200 | 250 | yes |
| 4 | Ana | completed | Keyboard 1 × 50 | 50 | yes |

| Statistic | Expected | How |
|---|---|---|
| Completed orders | **3** | orders 1, 3, 4 |
| Total sales | **425.00** | 125 + 250 + 50 |
| Average order value | **141.67** | 425 / 3 = 141.666… |
| Most popular product | **Keyboard and Mouse, 3 units each** | Keyboard 2 + 1, Mouse 1 + 2, Monitor 1 |

These numbers became the tests, written before the implementation, so the commit history shows them failing first.

I also listed what typical mistakes would print, so that each one can be recognised and caught by a test:

| Mistake | Wrong result |
|---|---|
| Counting the cancelled order | 4 orders, 475.00, average 118.75, Keyboard (4 units) |
| Picking one product from a tie | Keyboard only |
| Dividing total sales by all 4 orders | average 106.25 |
| Averaging per order line instead of per order | average 85.00 |
| Adding up prices without quantities | total 350.00 |
| Formatting numbers with the machine's culture | `141,67` on my Mac (region Georgia, .NET culture `en-GE`) |

_Test results, the check that each mistake above is caught by a test, and an independent cross-check are added after the implementation._

## Unclear cases and assumptions

| Case | Decision | Why | Test |
|---|---|---|---|
| Two products tie for most units sold | Report all of them, A→Z, with the unit count: `Keyboard, Mouse (tie, 3 units each)`. Units sold per product are printed too. | Picking one would be arbitrary. Here it also hides a bug: counting the cancelled order makes Keyboard win alone (4 units), so a bare "Keyboard" can't tell the two mistakes apart. | `All_products_tied_for_first_place_are_reported_alphabetically`, `A_single_top_product_is_reported_alone` |
| Which orders count in statistics | Only status `completed`, ignoring case and surrounding spaces. Any other status (`cancelled`, but also e.g. `pending` or the spelling `canceled`) is not counted, and the report says how many orders were left out per status. | Counting only `completed` can't let an unexpected status into revenue; a `status != "cancelled"` check would. | `Only_completed_orders_count_and_status_ignores_case_and_spaces`, `Cancelled_orders_do_not_change_any_statistic` |
| Cancelled orders in the list and in search | Shown, marked `(not counted in statistics)`. | Requirement 3 is about statistics; search should still find every order of a customer. | `Search_shows_cancelled_orders_marked_as_not_counted` |
| Average order value | Total sales ÷ number of completed orders, using `decimal`, rounded once at the end to 2 decimals, halves away from zero. | The usual definition. C#'s `Math.Round` defaults to banker's rounding (0.625 → 0.62). The provided data has no halfway case, so this changes nothing here; the rule is just explicit. | `Average_order_value_is_rounded_half_away_from_zero` |
| No completed orders | Total `0.00`; average and most popular product `n/a`. | The average is undefined, and dividing a `decimal` by zero throws. | `No_completed_orders_gives_zero_totals_and_no_average` |
| Customer search | Whole name, case-insensitive, surrounding spaces ignored; multi-word names allowed; no match prints a message. | People type names in any case; "Ana" must not find "Anano". | `Customer_search_matches_the_whole_name_ignoring_case_and_keeps_cancelled_orders` |
| Number format | Always `141.67`, whatever the machine's regional settings. | My Mac's culture is `en-GE` and would print `141,67`; so would Georgian `ka-GE`. | `Numbers_are_printed_with_a_dot_even_on_a_Georgian_locale` |
| Console encoding | Output is UTF-8; the program's own symbols are plain ASCII. | A Windows console not set to UTF-8 can show Georgian names as `?`. `orders.json` has Windows line endings, so reviewers may be on Windows. | manual, not testable on macOS |
| Invalid data | Stop with exit code 1 and a one-line message saying where and what: missing or null fields, wrong types, quantity below 1, negative price (0 is allowed, e.g. a free item), an order without items, a null order or item, a duplicate `orderId`, a blank customer, status or product. Unknown extra fields are ignored. | A wrong revenue figure is worse than none. | `Invalid_data_is_rejected_with_a_message_saying_where_and_what`, `Missing_file_exits_with_code_1_and_a_readable_message` |
| Identity | A product is identified by its exact name, a customer by their name. | The data has no IDs; two different customers with the same name would be merged. | — |
| Currency | None in the data, so amounts are plain numbers with 2 decimals. | — | — |
