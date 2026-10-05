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
| `dotnet test` | runs the 32 tests |

Exit codes: `0` success, `1` the file is missing or its data is invalid, `2` wrong command line.

## AI tools used

- **Claude Code** (desktop app, model Claude Opus 5.5) as the main assistant.
- Separate Claude sessions started from it with no shared context, used as independent reviewers: one critiqued the plan, one reviewed the finished code.

## What AI helped with

- **Planning:** analysing the data before coding. This surfaced the Keyboard/Mouse tie, the culture-dependent number format and the edge cases listed below.
- **Setup:** creating the .NET solution and the test project.
- **Tests and code:** writing the tests from my expected values first, then the implementation that makes them pass.
- **Verification tooling:** writing the script that re-introduces typical bugs to check the tests catch them.
- **Review:** an independent review of the finished code.
- **README:** drafting it.

I made the decisions (stack, how each unclear case is handled), checked the expected values by hand, reviewed the code and ran every step myself.

## Errors or wrong logic found in AI-suggested code

Yes. The implementation passed all 25 specification tests on its first run, but these problems were found along the way:

| Where | What was wrong | How it was found | Fix |
|---|---|---|---|
| First design (Python, later replaced by C#) | Money as `Decimal` via `json.load(parse_float=Decimal)` would have left every price in this file as `int` | Independent AI review of the plan | Not carried over: the final version uses C# `decimal`, which System.Text.Json parses exactly |
| Project setup | "Treat warnings as errors" on the test project turned NuGet's offline vulnerability-check warning (NU1900) into a build error: no build without internet or behind a proxy | First build | Warnings are errors only in the app project, which has no NuGet packages |
| Code | `--file ""` crashed with a stack trace | Independent AI code review | Blank path is a usage error (exit 2) |
| Code | Very large amounts (e.g. price 10²⁰) crashed with `OverflowException` | Independent AI code review | The loader adds up all order totals once; if that overflows, the file is rejected with a one-line message (exit 1). Prices are never negative, so no later sum can overflow. |
| Code | Units sold were added up in `int`: 2 × 2,000,000,000 units printed as `-294967296` and the wrong most popular product, with no error | Independent AI code review | Units are added up in `long` |
| Code | Tied products were sorted ordinally (`Mouse` before `keyboard`), contradicting "A→Z" below | Independent AI code review | Sorting ignores letter case |
| Verification script | One injected bug was meaningless: dividing by the number of distinct products (3) equals dividing by orders (3), so nothing could fail | Its result looked suspicious ("not caught") | Replaced with a real per-line average (÷ 5) |
| Verification script | Restoring a file gave it an older timestamp than the compiled program, so the build reused a broken binary and one test failed falsely | A test failed although `git status` showed no changes | Full rebuild for every run |

The code bugs only appear with unusual input; the results on the provided file were correct from the first run. Each fix was made test-first: a failing test, then the change.

## How I verified and fixed the results

**1. Expected results by hand, before any code.**

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

**2. What typical mistakes would print**, so they can be recognised:

| Mistake | Wrong result |
|---|---|
| Counting the cancelled order | 4 orders, 475.00, average 118.75, Keyboard (4 units) |
| Picking one product from a tie | Keyboard only |
| Dividing total sales by all 4 orders | average 106.25 |
| Averaging per order line instead of per order | average 85.00 |
| Adding up prices without quantities | total 350.00 |
| Formatting numbers with the machine's culture | `141,67` on my Mac (region Georgia, .NET culture `en-GE`) |

**3. Tests before code.** The expected values became tests, written against empty method stubs; the commit history shows them failing first and passing after the implementation.

**4. Do the tests actually catch mistakes?** A script injected 13 typical bugs one at a time, rebuilt, ran the tests and restored the code. Every bug made at least one test fail:

| Injected bug | Failing tests |
|---|---|
| Count every order, including cancelled | 8, incl. `Cancelled_orders_do_not_change_any_statistic` |
| `status != "cancelled"` instead of only `completed` | `Only_completed_orders_count_and_status_ignores_case_and_spaces` |
| Pick the first product of a tie | 4, incl. `All_products_tied_for_first_place_are_reported_alphabetically` |
| Divide by all orders | 4, incl. `Provided_file_gives_the_hand_computed_results` |
| Average per order line | 3, incl. `Provided_file_gives_the_hand_computed_results` |
| Ignore quantity | 5, incl. `Provided_file_gives_the_hand_computed_results` |
| `Math.Round` default (banker's rounding) | `Average_order_value_is_rounded_half_away_from_zero` |
| No guard for zero completed orders | `No_completed_orders_gives_zero_totals_and_no_average` |
| Culture-dependent number format | 4, incl. `Numbers_are_printed_with_a_dot_even_on_a_Georgian_locale` |
| Substring customer search | `Customer_search_matches_the_whole_name_ignoring_case_and_keeps_cancelled_orders` |
| Case-sensitive customer search | 2, incl. the one above |
| Accept quantity 0 | `Invalid_data_is_rejected_with_a_message_saying_where_and_what` |
| Accept duplicate `orderId` | `Invalid_data_is_rejected_with_a_message_saying_where_and_what` |

**5. Independent cross-check** with `jq`, which gives 3 orders, 425, 141.666… and Keyboard 3 / Mouse 3 / Monitor 1, the same as the program:

```
jq '[.[] | select(.status == "completed")] | {orders: length, total: ([.[].items[] | .quantity * .price] | add)}' orders.json
```

**6. Edits like a live change**, on copies of the file: a price of 19.99, status `" Completed "`, a Georgian customer name (`ნინო`), quantity `0`, quantity `"two"`, an empty array, a missing file, wrong arguments. Each gave the expected result or a one-line error.

**7. Independent code review** by a separate AI session, which found the bugs listed above. They were reproduced, covered by failing tests, fixed, and the bug-injection check was run again.

## Unclear cases and assumptions

| Case | Decision | Why | Test |
|---|---|---|---|
| Two products tie for most units sold | Report all of them, A→Z ignoring letter case, with the unit count: `Keyboard, Mouse (tie, 3 units each)`. Units sold per product are printed too. | Picking one would be arbitrary. Here it also hides a bug: counting the cancelled order makes Keyboard win alone (4 units), so a bare "Keyboard" can't tell the two mistakes apart. | `All_products_tied_for_first_place_are_reported_alphabetically`, `Tied_products_are_sorted_ignoring_letter_case`, `A_single_top_product_is_reported_alone` |
| Which orders count in statistics | Only status `completed`, ignoring case and surrounding spaces. Any other status (`cancelled`, but also e.g. `pending` or the spelling `canceled`) is not counted, and the report says how many orders were left out per status. | Counting only `completed` can't let an unexpected status into revenue; a `status != "cancelled"` check would. | `Only_completed_orders_count_and_status_ignores_case_and_spaces`, `Cancelled_orders_do_not_change_any_statistic` |
| Cancelled orders in the list and in search | Shown, marked `(not counted in statistics)`. | Requirement 3 is about statistics; search should still find every order of a customer. | `Search_shows_cancelled_orders_marked_as_not_counted` |
| Average order value | Total sales ÷ number of completed orders, using `decimal`, rounded once at the end to 2 decimals, halves away from zero. | The usual definition. C#'s `Math.Round` defaults to banker's rounding (0.625 → 0.62). The provided data has no halfway case, so this changes nothing here; the rule is just explicit. | `Average_order_value_is_rounded_half_away_from_zero` |
| No completed orders | Total `0.00`; average and most popular product `n/a`. | The average is undefined, and dividing a `decimal` by zero throws. | `No_completed_orders_gives_zero_totals_and_no_average` |
| Customer search | Whole name, case-insensitive, surrounding spaces ignored; multi-word names allowed; no match prints a message. | People type names in any case; "Ana" must not find "Anano". | `Customer_search_matches_the_whole_name_ignoring_case_and_keeps_cancelled_orders` |
| Number format | Always `141.67`, whatever the machine's regional settings. | My Mac's culture is `en-GE` and would print `141,67`; so would Georgian `ka-GE`. | `Numbers_are_printed_with_a_dot_even_on_a_Georgian_locale` |
| Console encoding | Output is UTF-8; the program's own symbols are plain ASCII. | A Windows console not set to UTF-8 can show Georgian names as `?`. `orders.json` has Windows line endings, so reviewers may be on Windows. | manual, not testable on macOS |
| Invalid data | Stop with exit code 1 and a one-line message saying where and what: missing or null fields, wrong types, quantity below 1, negative price (0 is allowed, e.g. a free item), an order without items, a null order or item, a duplicate `orderId`, a blank customer, status or product, amounts too large to add up. Unknown extra fields are ignored. | A wrong revenue figure is worse than none. | `Invalid_data_is_rejected_with_a_message_saying_where_and_what`, `Missing_file_exits_with_code_1_and_a_readable_message` |
| Identity | A product is identified by its exact name, a customer by their name. | The data has no IDs; two different customers with the same name would be merged. | — |
| Currency | None in the data, so amounts are plain numbers with 2 decimals. | — | — |
