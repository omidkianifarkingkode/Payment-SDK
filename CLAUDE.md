# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a **Unity 2022.3.62f3 SDK** (not a standalone game) that integrates the Zarinpal payment gateway into Unity mobile games. The backend REST API contract is documented in `Docs/Payment Service Doc.md`.

## Running Tests

Tests are **Edit Mode only** and must be run through the Unity Editor:

> Window → General → Test Runner → Edit Mode → Run All

There is no CLI test runner. The test assembly is `com.kingkode.payment-sdk.tests.editmode` and only compiles when `UNITY_INCLUDE_TESTS` is defined (Unity handles this automatically in the editor).

## Architecture

### Two Integration Modes

**Direct API mode** — the default. Use `GamePayment` (static façade) backed by `GamePaymentController`.

**Unity IAP mode** — wraps the SDK as a `UnityEngine.Purchasing` store via `GamePaymentIapModule` → `GamePaymentIapStore`. Requires the `GAME_PAYMENT_UNITY_IAP` define symbol. See `PaymentBootstrapUnityIap.cs` for wiring.

### Assembly Structure

| Assembly | Purpose |
|---|---|
| `com.kingkode.payment-sdk` | Core runtime: all services, API client, storage, models |
| `com.kingkode.payment-sdk.uniwebview` | `UniWebViewPaymentService` — production WebView on device |
| `com.kingkode.payment-sdk.unity-iap` | Unity IAP store adapter |
| `com.kingkode.payment-sdk.tests.editmode` | NUnit Edit Mode tests |

### Service Dependency Graph

```
GamePayment (static)
  └─ GamePaymentController
       ├─ PaymentApiClient          (UnityWebRequest, x-api-key header)
       ├─ ProductCatalogService     → ProductCatalogCache
       ├─ PaymentRequestService     → IPendingOrderStorage
       ├─ PaymentClaimService       → IPendingOrderStorage
       ├─ PaymentPurchaseFlowService
       │    ├─ IPaymentWebViewService   (UniWebViewPaymentService | MockPaymentWebViewService)
       │    └─ PaymentCallbackParser
       ├─ PlayerPrefsPendingOrderStorage   (keyed by playerId)
       ├─ PlayerPrefsProcessedTransactionStorage
       └─ PaymentLocalCleanupService
```

### Purchase Flow (Critical to Understand)

1. `GamePayment.PurchaseAsync(productKey)` → `POST /v1/payments/request` → save `PendingOrder` locally
2. Open WebView at `paymentUrl`; `PaymentCallbackParser` watches every URL change for `/v1/payments/callback/`
3. On callback with `status=OK` → `POST /v1/payments/claim` → fire `PurchaseSucceeded` event
4. **Game code must call `GamePayment.ConfirmPurchaseProcessed(result)` after granting the reward.** Without this the order stays in `ProcessedTransactionStorage` as unprocessed and will re-fire on recovery.

### Pending Order Recovery

On every `InitializeAsync`, after the product catalog loads, `ClaimLocalPendingOrdersAsync` runs automatically. This recovers orders from previous sessions where the WebView was closed before the claim completed. The backend claim endpoint (`POST /v1/payments/claim` with `orderId: null`) returns all unclaimed verified orders for the player.

### WebView Abstraction

`IPaymentWebViewService` has three events (`UrlChanged`, `ClosedByUser`, `LoadFailed`) and two methods (`Open`, `Close`).

- **Production**: assign `UniWebViewPaymentService` (from `com.kingkode.payment-sdk.uniwebview`) to the `_deviceWebViewService` field.
- **Editor testing**: assign `MockPaymentWebViewService` to `_mockWebViewService`. It auto-fires a success or cancel callback after a configurable delay. In the Editor the service is selected via `#if UNITY_EDITOR`.

### Callback Detection

`PaymentCallbackParser` identifies callbacks by checking whether the URL path contains `/v1/payments/callback/`. The query parameters `authority` and `status` (OK/NOK) are parsed from the URL. The callback is detected client-side by monitoring WebView URL changes — the server redirects the browser after Zarinpal posts back.

### Local Storage

`PlayerPrefsPendingOrderStorage` and `PlayerPrefsProcessedTransactionStorage` both scope their PlayerPrefs keys by `playerId` to prevent cross-player contamination on shared devices. Pending orders older than `PendingOrderCleanupDays` (default: 7) are removed on startup.

## Key Configuration Fields

```csharp
new PaymentConfiguration
{
    BaseUrl = "https://...",      // required, trailing slash stripped
    ApiKey  = "game_live_...",    // sent as x-api-key header
    Environment = PaymentEnvironment.Production,
    RequestTimeoutSeconds  = 20,
    WebViewTimeoutSeconds  = 300,
    ClaimRetryCount        = 3,
    PendingOrderCleanupDays = 7,
    ProcessedTransactionHistoryDays = 90,
    EnableLogs = true
}
```

## Backend API Contract (Summary)

All game-client endpoints require `X-Api-Key` header. Full spec: `Docs/Payment Service Doc.md`.

| Method | Path | Purpose |
|---|---|---|
| GET | `/v1/products` | Fetch active product catalog |
| POST | `/v1/payments/request` | Create order, get `paymentUrl` |
| GET | `/v1/payments/callback/{clientId}?authority&status` | Zarinpal redirects here after payment |
| POST | `/v1/payments/claim` | Claim verified order(s); `orderId: null` claims all pending |

Order lifecycle: `Created → Pending → Paid → Verified → Claimed` (failure path ends at `Failed`).

## Sample Scene Wiring

`Assets/Payment SDK/Samples/` contains two ready-made bootstrap MonoBehaviours:

- `PaymentBootstrapDirect.cs` — Direct API mode with product list UI
- `PaymentBootstrapUnityIap.cs` — Unity IAP mode (guarded by `#if GAME_PAYMENT_UNITY_IAP`)

`Assets/Resources/BillingMode.json` configures the Android store (currently `"GooglePlay"`).
