# Game Payment Service
Product & Technical Specification
Version: 1.0
Scope: Phase 1 and 2
Gateway: Zarinpal

## Product Overview
This is an independent "Payment Service" for games that facilitates in-app purchases through a payment gateway.
This service acts as an intermediary between the game client and the payment gateway, performing the following tasks:
1. Managing store products
2. Creating orders
3. Interacting with the payment gateway
4. Verifying payments
5. Managing order statuses
6. Granting access to purchased items

-------------------------------------------------------------------------

# Definitions

## System Actors
- **Player**: The user who makes purchases within the game.
- **Game Client**: The game application (Unity).
- **Payment Service**: The service that manages the purchase process.
- **Payment Gateway**: The payment gateway (Zarinpal).
- **Support Agent**: Support operator who reviews users' payment issues.
- **QA Tester**: Tester who simulates payments in a test environment.

## Epics
- **Client Management**: Manage games using this service.
- **Product Management**: Manage purchaseable products within the game.
- **Payment Core**: Fully manage the payment flow.
- **Test Environment**: Enable testing of payments without real money.
- **Support Tools**: Review and compensate purchases for the support team.

-------------------------------------------------------------------------

# Phases

## Phase 1 Objectives:
The goal of this phase is to create a Minimum Viable Payment System that:
- Is production-ready
- Is simple and extensible
- Has minimal architectural complexity

### Out of Scope for Phase 1:
The following features will be implemented in subsequent phases:
- Player authorization
- Event-driven architecture
- Queue/outbox pattern
- Fraud detection
- Analytics pipeline

## Phase 2 Objectives:
The goal of this phase is to provide operational tools for the support team and QA.
- Testing payments without using real money
- Reviewing and searching for transactions by the support team
- Manually verifying payments
- Granting prizes manually if there are issues

### Out of Scope for Phase 2:
The following features will be implemented in subsequent phases:
- Player authorization
- Event-driven architecture
- Queue/outbox pattern
- Fraud detection
- Analytics pipeline

-------------------------------------------------------------------------

# User Stories

## 1. Register a new Client
** As a system admin, I want to register a new client (game) so that it can use the payment service.**

### Epic
*Client Management*

### Acceptance Criteria
- `clientId` must be unique for each registered client.
- Generate a random 32-character string for the `apiKey`.
- Store both `clientId` and `apiKey` in your database.
- CRUD api + /disable
- Dashboard View

### Endpoint Design

```http
POST /v1/clients
Content-Type: application/json
Authorization: super-admin-token
```
Request Body
```json
{
  "name": "MyGame"
}
```
Response
```http
HTTP/1.1 201 Created
Content-Type: application/json
```
Body
```json
{
  "clientId": "client_123",
  "apiKey": "game_live_xxxxxx"
}
```

### Error Codes
400 BAD_REQUEST: Invalid format.
401 UNAUTHORIZED: super-admin-token invalid
404 NOT_FOUND: Client not found.

### Technical Notes
* The API key should be included in the request header with the name "x-api-key" when making payment requests.
* api-key can not be update

----------------------------------------------------------

## 2. Define a New Product
** As a game admin, I want to define a new product for a client so that players can purchase it.**

### Epic
*Product Management*

### Acceptance Criteria
- `productKey` must be unique for each defined product.
- Price must be a positive decimal number.
- Product must be active by default.
- CRUD api + /disable
- Dashboard View

### Endpoint Design

```http
POST /v1/clients/{clientId}/products
Content-Type: application/json
Authorization: admin-token
```
Request Body
```json
{
  "productKey": "gem_pack_small",
  "name": "Small Gem Pack",
  "price": 50000,
  "currency": "IRR" | "IRT"
}
```
Response
```http
HTTP/1.1 201 Created
Content-Type: application/json
```
Body
```json
{
  "id": "ProductId",
  "productKey": "gem_pack_small"
}
```

### Error Codes
400 BAD_REQUEST: Price must be > 0.
401 UNAUTHORIZED: admin-token invalid
404 NOT_FOUND: Product not found.
409 CONFLICT: ProductKey already exists.

### Technical Notes

----------------------------------------------------------

## 3. View List of Products
** As a player, I want to view the list of products so that I can make a purchase.**

### Epic
*Product Management*

### Acceptance Criteria
- Only return active products.
- Price must be a positive decimal number.
- The response must be fast (<100ms).

### Endpoint Design

```http
GET /v1/products
X-Api-Key: api-key
```
Response
```http
HTTP/1.1 200 OK
Content-Type: application/json
```
Body
```json
{
  "products": [
    {
      "productKey": "gem_pack_small",
      "name": "Small Gem Pack",
      "price": 50000,
      "currency": "IRR"
    }
  ]
}
```

### Error Codes
401 UNAUTHORIZED: api-key invalid
403 FORBIDDEN: client is disable

### Technical Notes

----------------------------------------------------------

## 4. Request Payment for a Product
** As a player, I want to request payment for a product so that I can be redirected to the payment gateway.**

### Epic
*Payment Core*

### Acceptance Criteria
- Order must persist in the database.
- Authority must be persisted in the database.
- Return payment URL.

### Endpoint Design

```http
POST /v1/payments/request
Content-Type: application/json
X-Api-Key: api-key
```
Request Body
```json
{
  "playerId": "player123",
  "productKey": "gem_pack_small"
}
```
Response
```http
HTTP/1.1 200 OK
Content-Type: application/json
```
Body
```json
{
  "orderId": "ord_123",
  "paymentUrl": "https://www.zarinpal.com/pg/StartPay/A000..."
}
```

### Error Codes
400 BAD_REQUEST: Invalid productKey.
401 UNAUTHORIZED: api-key invalid
403 FORBIDDEN: client is disable
404 NOT_FOUND: Product not found.
502 BAD_GATEWAY: Zarinpal API failure.

### Technical Notes
- Bussines Flow:
	1. Find product.
	2. Create order.
	3. Call Zarinpal request.
	4. Save authority.
	5. Return payment URL.
- Refrence Doc: https://www.zarinpal.com/docs/paymentGateway/moreFeatures/checkout.html
    * Fill zarinpal.cart_data for more clear payment
    * Fill zarinpal.meta.orderId with `orderId`
    * Fill zarinpal.description with product.description

----------------------------------------------------------

## 5. Handle Payment Callback
** As a payment system, I want to handle the payment result from the gateway so that I can verify and update the order status.**

### Epic
*Payment Core*

### Acceptance Criteria
- Always verify.
- Persist `refId`.
- Update order status.

### Endpoint Design

```http
GET /v1/payments/callback/{clientId}?authority&status
```

Response
```http
HTTP/1.1 200 OK
Content-Type: application/json
```
Body
```json
{
  "status": "verified"
}
```

### Error Codes
400 BAD_REQUEST: Invalid authority or status.
404 NOT_FOUND: Client not found.
500 INTERNAL_SERVER_ERROR: Zarinpal API failure.

### Technical Notes
- Bussines Flow:
	1. Find order.
	2. If `status` is "NOK", Update `order.status` = "Failed" and finish process
	3. Else Update `order.status` = "Paid"
	4. Call Zarinpal Verify API
	5. Add `response.card_pan` to `order.meta`
	6. Store `response.refId` to `order.refId`
- callback may happen multi time -> if status == verified then ignore it
- Refrence Doc: https://www.zarinpal.com/docs/paymentGateway/connectToGateway.html

----------------------------------------------------------

## 6. Claim Purchase Reward
** As a player, I want to claim my successful purchase reward.**

### Epic
*Payment Core*

### Acceptance Criteria
- Order must be paid.
- Order must not have been claimed before.
- Set `claimed=true`.

### Endpoint Design

```http
POST /v1/payments/claim
Content-Type: application/json
X-Api-Key: api-key
```
Request Body
```json
{
  "playerId": "player123"
}
```
Response
```http
HTTP/1.1 200 OK
Content-Type: application/json
```
Body
```json
[
	{
		"orderId": "ord_123",
		"productKey": "gem_pack_small"
	}
]
```
### Error Codes
400 BAD_REQUEST: Payment not verified.
401 UNAUTHORIZED: api-key invalid
403 FORBIDDEN: client is disable
409 CONFLICT: Already claimed.
404 NOT_FOUND: Order not found.

### Technical Notes
- if payment not verified, verify and check status then response
- claim process must be atomic to protect double reward

----------------------------------------------------------

## 7. Create Test User
** As a game admin, I want to create a test user so that I can simulate payments in the testing environment.**

### Epic
*Test Environment*

### Acceptance Criteria
- `playerId` should be recorded in the testUsers table.
- `testUser=true` should be saved.
- CRUD api + /disable
- Dashboard View

### Endpoint Design

```http
POST /v1/clients/{clientId}/test-users
Content-Type: application/json
Authorization: admin-token
```
Request Body
```json
{
  "playerId": "test_player_1"
}
```
Response
```http
HTTP/1.1 201 Created
Content-Type: application/json
```
Body
```json
{
  "status": "created"
}
```
### Error Codes
400 BAD_REQUEST: playerId invalid.
401 UNAUTHORIZED: admin-token invalid
404 NOT_FOUND: Client not found.
409 CONFLICT: playerId already exists.

### Technical Notes
- Test users are allowed to:
	* Mock payments
	* Skip gateway
	
----------------------------------------------------------

## 8. Create Mock Payment
** As a QA Tester, I want to create a mock payment so that I can examine the system's behavior without using the real gateway.**

### Epic
*Test Environment*

### Acceptance Criteria
- Same as real payment flow

----------------------------------------------------------

## 9. Search Payments
** As a Support Agent, I want to search user payments so that I can investigate payment issues.**

### Epic
*Support Tools*

### Acceptance Criteria
- Ability to search with multiple filters.
- Support for pagination.
- Dashboard View

### Endpoint Design

```http
GET /v1/support/payments?playerId=player123&fromDate=YYYY-MM-DD&toDate=YYYY-MM-DD&refId=1234&authority=A000
Authorization: support-panel-token | admin-auth
```

Response
```http
HTTP/1.1 200 OK
Content-Type: application/json
```
Body
```json
{
  "payments": [
    {
      "orderId": "ord_123",
      "playerId": "player123",
      "productKey": "gem_pack_small",
      "amount": 50000,
      "status": "Paid",
      "claimed": false
    }
  ]
}
```
### Error Codes
400 BAD_REQUEST: Invalid filter.
401 UNAUTHORIZED: support-panel-token invalid

### Technical Notes

----------------------------------------------------------

## 10. Manual Payment Verification
** As a Support Agent, I want to manually verify a payment so that the payment status can be specified in case of losing callback.**

### Epic
*Support Tools*

### Acceptance Criteria
- The verify API of the gateway should be called.
- Order status should be updated.
- Dashboard View

### Endpoint Design

```http
POST /v1/support/payments/validate
Content-Type: application/json
Authorization: support-panel-token | admin-auth
```
Request Body
```json
{
  "authority": "A000000000000",
  "supportAgent" : "Agent1"
  "reason" : "issue description",
}
```
Response
```http
HTTP/1.1 200 OK
Content-Type: application/json
```
Body
```json
{
  "status": "Paid",
  "refId": "12345678"
}
```
### Error Codes
400 BAD_REQUEST: Invalid request.
401 UNAUTHORIZED: support-panel-token invalid
404 NOT_FOUND: authority not found.
500 INTERNAL_SERVER_ERROR: Zarinpal API failure.

### Technical Notes
- Bussines Flow:
	1. Find order.
	2. Call inquiry API.
	3. Update status.
	4. Save refId.
- https://www.zarinpal.com/docs/paymentGateway/otherMethods/Inquiry.html#%D8%A7%D8%B1%D8%B3%D8%A7%D9%84-%D8%A7%D8%B7%D9%84%D8%A7%D8%B9%D8%A7%D8%AA
	
----------------------------------------------------------

## 11. Manual Reward Grant
** As a Support Agent, I want to manually grant a purchase reward so that the user's purchase can be compensated in case of error.**

### Epic
*Support Tools*

### Acceptance Criteria
- Order should be `Paid`.
- Order should not have been claimed before.
- Set `claimed=true`.
- Dashboard View

### Endpoint Design

```http
POST /v1/support/payments/grant
Content-Type: application/json
Authorization: support-panel-token | admin-auth
```
Request Body
```json
{
  "orderId": "ord_123",
  "playerId": "player123",
  "supportAgent" : "Agent1"
  "reason" : "issue description",
}
```
Response
```http
HTTP/1.1 200 OK
Content-Type: application/json
```
Body
```json
{
  "status": "reward_granted"
}
```
### Error Codes
400 BAD_REQUEST: Invalid request.
401 UNAUTHORIZED: support-panel-token invalid
404 NOT_FOUND: orderId or playerId not found.
405: Payment not verified.
409: Already claimed.

### Technical Notes
- Validate  `orderId` matches  `playerId`

----------------------------------------------------------

# High Level Flow

## Payment Flow

1.  Player              ------- Click `Purchase` Button --------> Unity Client
2.  Unity Client        ------- POST /payments/request ---------> Payment Service
3.  Payment Service     -------- payment.zarinpal.com ----------> Zarinpal
4.  Zarinpal            ----------- Return `authority`----------> Payment Service
5.  Payment Service     ----------- Return `paymentUrl`---------> Unity Client
6.  Unity Client        ------------ Open `paymentUrl`----------> Zarinpal
7.  Player              ------------ Pay in Webview ------------> Zarinpal
8.  Zarinpal            ------- Redir /payments/callback -------> Payment Service
9.  Payment Service     --------- payment.zarinpal.com ---------> Zarinpal
10. Zarinpal            ------------ Return Success ------------> Payment Service
11. Payment Service     ------- Show Payment Result View -------> Unity Client
12. Player              ------ Click `Back to App` Button ------> Unity Client (JS)
13. Unity Client        --------- POST /payments/claim ---------> Payment Service
14. Payment Service     ------------ Return Success ------------> Unity Client
15. Unity Client        ----------- Grant **Reward** -----------> Player

## Order Lifecycle

### Normal Path
1. Created  : Order generated, but user hasn’t reached the payment gateway (Authority received).
2. Pending  : User redirected to payment gateway; payment may be completed or canceled.
3. Paid     : Zarinpal callback received, but verification pending.
4. Verified : Payment successfully verified (RefID = Bank Reference).
5. Claimed  : Reward granted to the player.

### Failure Path
1. Created
2. Pending
3. Failed : Payment unsuccessful (user canceled, payment failed, or verification failed).


----------------------------------------------------------

# Edge Cases

## User Presses Back and Pays Again
### Scenario
1. User initiates payment.
2. Completes payment once.
3. Presses back or refreshes the page.
4. Attempts to pay again.
- OR Unity Client sends a duplicate payment request.
### Problem
- Duplicate payments may process, leading to:
	* Double charges on the user’s account.
	* Inconsistent order records.
	* Potential fraud if Zarinpal returns the same authority (transaction ID).
### Solution
- Allow duplicate requests (since real money is involved).
- Track orders by authority (Zarinpal’s transaction ID).
- Log repeated payments as potential fraud if the same authority is reused.
- Ensure Unity Client enforces a cooldown (e.g., prevent rapid retries).
- Payment Service should:
	* Accept multiple requests but only process the first valid payment.
	* Mark subsequent attempts as duplicate attempts (without charging again).
	* Verify via Zarinpal API before finalizing rewards.

## User Refreshes Bank Page
### Scenario
1. User completes payment in Zarinpal’s gateway.
2. Refreshes the payment result page (e.g., due to browser cache or manual refresh).
3. Callback is triggered multiple times.
### Problem
- Duplicate callbacks to /payments/callback:
	* May cause race conditions in the Payment Service.
	* Risk of double-processing the same transaction.
	* Potential state inconsistency (e.g., marking an order as Verified twice).
### Solution
- Make the callback idempotent:
	* Check the status (e.g., Paid, Verified) before processing.
	* Ignore subsequent callbacks if the order is already in a final state (Paid, Verified or Claimed).
	* Use a transaction lock (e.g., database row lock or RefId check) to prevent race conditions.
	* Log duplicates for auditing (without reprocessing).

## Fake Callback Protection
### Scenario
1. An attacker or malicious user directly invokes the /payments/callback endpoint
2. Callback is not inherently trusted (vulnerable to spoofing).
### Problem
- Unverified callbacks can lead to:
	* False positives (e.g., marking an order as Paid without actual payment).
	* Financial fraud (e.g., rewarding users for fake transactions).
	* System instability (race conditions if the service blindly updates order status).
	* ref: https://www.zarinpal.com/docs/paymentGateway/otherMethods/unVerified.html
### Solution
- Secure Callback Flow
- Never trust the callback directly. Always verify with Zarinpal’s API before processing.
- Log failed verifications for auditing.
- Use rate limiting to prevent brute-force attacks on /payments/callback.
- Implement CSRF protection if the callback is exposed to user-controlled URLs.

## Authority Reuse Protection
### Scenario
- Zarinpal authority values may be:
	* Reused (accidentally or due to system quirks)
	* Maliciously replayed (by attackers to trigger duplicate payments)
### Problem
- Duplicate authority values could lead to:
	* Double payments processed for the same transaction
	* Inconsistent order states (e.g., marking as Paid twice)
	* Financial discrepancies in bank reconciliation
### Solution
- Strict Authority Validation
	* Rule: Each authority should only validate one unique order
- Implementation Checks
	1. Verify order status first:
	```pseudo
	if order.status == "Paid" or order.status == "Verified":
    ignore this callback
	```
	2. Process flow for valid callbacks:
	```
	1. Callback received with authority=XXX
	2. Find order by authority
	3. If order exists AND status != "Paid":
	a. Verify with Zarinpal API
	b. If successful:
		- Set status = "Paid"
		- Mark as "Verified" after successful verification
	c. If failed:
		- Set status = "Failed"
	4. If order doesn't exist:
	- Create new order with status = "Pending"
	- Proceed with verification
	```
- Database constraint: Add unique index on authority column
- Logging: Track all authority reuse attempts
- Rate limiting: Prevent brute-force authority replay attacks
- Audit trail: Maintain complete transaction history for all authorities

## Replay Attack Protection (Client-Side)
### Scenario
- Malicious player or buggy Unity client could replay the /payments/request
- This could create duplicate orders (e.g., 10 identical orders for one payment).
### Problem
- Resource exhaustion (server processing duplicate requests).
- Financial fraud (multiple pending orders for same payment).
- User experience issues (confusing multiple payment attempts).
### Solution
- Strict Rate Limiting
	* Rate limit by unique key: (playerId + productId)  // Unique combination per payment attempt
	* Time window: 30-second sliding window (adjustable).
	* Rate limit: Maximum 3 requests per window per player-product pair.
- Server-Side Checks
	```pseudo
	function handlePaymentRequest(playerId, productId):
    key = playerId + ":" + productId
    if requestCount[key] >= 3:
        return error("Too many requests. Try again later.")

    if orderExists(key) and order.status == "Pending":
        return existingOrder  // Return existing pending order

    createNewOrder(key)
    increment requestCount[key]
    resetTimer(key)  // Reset counter for new window
	```
- Database-backed rate limiting (for high-scale systems).
- Cache invalidation after successful payment.
- Logging of rate limit violations.
- Graceful degradation (return existing order instead of error).

## Double Claim Protection
### Scenario
- Highest risk: A user could receive the reward twice for the same order.
- Possible attack vectors:
	* Client-side replay of claim requests
	* Race conditions during concurrent claims
	* Database inconsistency during high load
### Problem

### Solution
- Atomic Claim Prevention
	* Single claim per order (idempotent operation)
	* Database-level protection against race conditions
- Database Schema
	```sql
	CONSTRAINT unique_player_product UNIQUE (player_id, product_id)
	```
- Atomic Claim Logic (Pseudo-code)
	```pseudo
	BEGIN TRANSACTION;

	-- Lock the specific order to prevent concurrent modifications
	SELECT * FROM Orders
	WHERE id = :order_id FOR UPDATE;
	
	-- Check if already claimed
	IF claimed = true THEN
		ROLLBACK;
		RETURN AlreadyClaimedError;
	
	-- Grant reward (application logic)
	CALL grantRewardToPlayer(:player_id);
	
	-- Update status atomically
	UPDATE Orders
	SET
		claimed = true,
		claimed_at = NOW()
	WHERE id = :order_id;
	
	COMMIT;
	```
- Row-level locking (FOR UPDATE) prevents race conditions

## Amount Tampering Protection
### Scenario
- Malicious client manipulation:
	* User modifies request payload to change amount
	* Example tampered request:
	```json
	{
		"productId": "gem_pack_small",
		"amount": 100  // Arbitrary value injected
	}
	```
- Potential attack vectors:
	* Price manipulation (requesting higher/lower amounts)
	* Currency conversion attacks
	* Bypassing pricing logic
### Problem
- Security vulnerabilities:
	* Financial fraud: Users could request inflated amounts
	* System inconsistencies: Pricing logic bypassed
	* Audit failures: No traceability of original prices
- Operational risks:
	* Pricing errors in inventory systems
	* Revenue discrepancies
	* Difficulty in fraud detection
### Solution
- Server-Side Price Validation:
	* Never trust client-provided amounts
	*  Always derive price from server-side product data
	*  Immutable pricing structure

##  Expired Order Protection
### Scenario
- Delayed payment processing:
	* User initiates payment but delays completing it (e.g., leaves app, loses connection)
	* Network issues or user distraction cause payment to be attempted after order creation
	* System clock synchronization issues (rare but possible)
### Problem
- Financial risks:
	* Processing stale orders may lead to duplicate payments
	* Inconsistent inventory management (selling already "paid" items)
- Operational challenges:
	* Difficulty tracking valid vs. expired transactions
	* Potential for fraudulent reuse of expired order references
- User experience issues:
	* Failed payments after legitimate delays
	* Inconsistent system behavior
### Solution
- Set automatic expiration (e.g., 30 minutes from creation).
- Accept payment if expired but still valid (most systems use this approach).
- Key Rules:
	* Orders expire after 30 minutes
	* Expired orders still process payments
	* Prevents fraud while maintaining usability

## RefId Validation
### Scenario
- User submits payment with duplicate refId.
### Problem
- Duplicate transactions risk fraud or double charges.
### Solution
- Enforce UNIQUE(refId) in database.
- If duplicate → Log alert + reject transaction.

## Product Validation
### Scenario
- Invalid productId submitted.
### Problem
- System crashes or sells non-existent items.
### Solution
- Check Products table for Id, ClientId, IsActive.
- If invalid → Return error.

## User Spam Protection
### Scenario
- User rapidly taps purchase button.
### Problem
- Floods server with duplicate requests.
### Solution
- Rate limit: 3 payment requests/minute/player.
- Claim limit: 10/minute.

##  WebView Close Protection
### Scenario
- User closes WebView before payment confirmation.
### Problem
- Unity never receives redirect.
### Solution
- Store pending claims locally.
- Auto-claim on app restart.

## Internet Disconnect After Payment
### Scenario
- Payment succeeds but internet drops.
### Problem
- Game can’t call claim.
### Solution
- Mark order as Paid if callback fails.
- Claim on next login.

## Client Calls Claim Before Verify
### Scenario
- Unity calls claim before payment verification.
### Problem
- Fails due to unprocessed verify.
### Solution
- Check status before claiming.
- If not Verified → Retry verify.

## Gateway Verify Timeout
### Scenario
- Callback fails due to timeout.
### Problem
- Payment stuck in limbo.
### Solution
- Retry verify 3 times (5 sec delay).
- If fails → Mark as Failed.

## Callback Never Arrives
### Scenario
- Payment succeeds but no callback.
### Problem
- Transaction never confirmed.
### Solution
- Claim triggers verify if no callback.
- If Verified → Process payment.

## Gateway Success but Verify Fails
### Scenario
- Callback says success, but verify fails.
### Problem
- Payment status unclear.
### Solution
- Only verify status matters.
- If verify fails → Order = Failed.

## Duplicate Callback
### Scenario
- Gateway sends same callback multiple times.
### Problem
- Duplicate processing.
### Solution
- Ignore if status already Paid.

## Payment After Order Expiration
### Scenario
- User pays 2+ hours after order creation.
### Problem
- Expired orders rejected.
### Solution
- Accept payment (real money received).
- Update status to Paid.



----------------------------------------------------------

# Database Design

## Clients
Column            | Type             | Constraints         
------------------|------------------|--------------
Id                | UNIQUEIDENTIFIER | PRIMARY KEY 
Name              | VARCHAR(255)     | NOT NULL            
ApiKey            | CHAR(32)         | UNIQUE, NOT NULL    
MerchantId        | VARCHAR          | NOT NULL                    
SandboxMerchantId | VARCHAR          |                     
IsActive          | BOOLEAN          | DEFAULT TRUE        
CreatedAt         | DATETIME         | DEFAULT CURRENT_TIMESTAMP 

## Products
Column            | Type             | Constraints         
------------------|------------------|--------------
Id                | UNIQUEIDENTIFIER | PRIMARY KEY 
ClientId          | UNIQUEIDENTIFIER | NOT NULL, FOREIGN KEY REFERENCES Clients(Id)
ProductKey        | VARCHAR(255)     | NOT NULL            
Name              | VARCHAR(255)     | NOT NULL            
Price             | DECIMAL          | NOT NULL            
Currency          | CHAR(3)          | NOT NULL            
IsActive          | BOOLEAN          | DEFAULT TRUE        
CreatedAt         | DATETIME         | DEFAULT CURRENT_TIMESTAMP 
UpdatedAt         | DATETIME         |

Constraints:
- UNIQUE KEY ClientId_ProductKey (ClientId, ProductKey)
- Price Min Value

## Orders
Column            | Type             | Constraints         
------------------|------------------|--------------
Id                | UNIQUEIDENTIFIER | PRIMARY KEY                                                       
ClientId          | UNIQUEIDENTIFIER | NOT NULL, FOREIGN KEY REFERENCES Clients(Id)                                                                                                             |
ProductId         | UNIQUEIDENTIFIER | NOT NULL, FOREIGN KEY REFERENCES Products(Id)                                     
PlayerId          | VARCHAR(255)     | INDEX     
Amount            | DECIMAL          | NOT NULL                                                                          
Authority         | VARCHAR(255)     | UNIQUE, INDEX                                                                     
RefId             | VARCHAR(255)     | UNIQUE IF NOT NULL      
Meta              | VARCHAR(Max)     | Json
Status            | VARCHAR(50)      | DEFAULT 'Created', INDEX
IsSandbox         | BOOLEAN          | DEFAULT FALSE                                                                     
CreatedAt         | DATETIME         | DEFAULT CURRENT_TIMESTAMP                                                         
PaidAt            | DATETIME         |                                                                                   
ClaimedAt         | DATETIME         |                                                                                   

Constraints:
- Status ENUM('Created', 'Pending', 'Paid', 'Verified', 'Failed', 'Claimed')

## TestUsers
Column            | Type             | Constraints         
------------------|------------------|--------------
Id                | UNIQUEIDENTIFIER | PRIMARY KEY 
PlayerId          | VARCHAR(255)     | NOT NULL            
ClientId          | UNIQUEIDENTIFIER | NOT NULL, FOREIGN KEY REFERENCES Clients(Id)
IsActive          | BOOLEAN          | DEFAULT TRUE        
CreatedAt         | DATETIME         | DEFAULT CURRENT_TIMESTAMP 

Constraints:
- UNIQUE KEY PlayerId_ClientId (PlayerId, ClientId)

## SupportActions
Column            | Type             | Constraints         
------------------|------------------|--------------
Id                | UNIQUEIDENTIFIER | PRIMARY KEY 
OrderId           | UNIQUEIDENTIFIER | NOT NULL, FOREIGN KEY REFERENCES Orders(Id)
SupportAgent      | VARCHAR(255)     | NOT NULL            
Reason            | NVARCHAR(1000)   |                     
Action            | VARCHAR(50)      | NOT NULL 
CreatedAt         | DATETIME         | DEFAULT CURRENT_TIMESTAMP 

Constraints:
- Action ENUM('VerifyPayment', 'GrantReward')