# Address to Coordinates – AWS Lambda with Google Geocoding Cache

## Overview

This project implements an **enterprise‑ready, serverless geocoding service** using **AWS Lambda (.NET 7)**, **Amazon API Gateway**, **Amazon DynamoDB**, and the **Google Geocoding API**.

The Lambda function exposes a simple HTTP GET endpoint that:

1. Receives a U.S. address as a query parameter.
2. Checks DynamoDB for a cached Google Geocoding response.
3. Returns the cached response if it exists and is still valid (30‑day TTL).
4. Otherwise, calls the Google Geocoding API, stores the full response in DynamoDB, and returns it.

The goal of this exercise is not only functionality, but **clean architecture, separation of concerns, and production‑grade design decisions**.

---

## High‑Level Architecture

```
Client
  ↓
API Gateway (HTTP GET /geocode)
  ↓
AWS Lambda (.NET 7)
  ├── Cache hit → DynamoDB
  └── Cache miss → Google Geocoding API → DynamoDB
```

### Key Design Decisions

* **Serverless-first**: No servers to manage, scales automatically.
* **Cache-aside pattern** using DynamoDB.
* **Full Google API response stored**, not partial data.
* **TTL-based cache expiration** (30 days) enforced by DynamoDB.
* **Clear separation of responsibilities** (Function, Repository, External Client).

---

## API Usage

### Endpoint

```
GET /geocode?address={US_ADDRESS}
```

### Example Request

```
https://djs6l6d7xf.execute-api.us-east-1.amazonaws.com/geocode?address=400+Broad+St,+Seattle,+WA+98109
```

### Example Response (Google API – full payload)

```json
{
  "results": [...],
  "status": "OK"
}
```

### Cache Behavior

* **First request** → Calls Google API → Stores response in DynamoDB
* **Subsequent requests (within 30 days)** → Returned directly from DynamoDB
* DynamoDB TTL attribute automatically expires cached entries

---

## DynamoDB Schema

| Attribute      | Type   | Description                             |
| -------------- | ------ | --------------------------------------- |
| `address`      | String | Partition key (US address)              |
| `responseJson` | String | Full Google Geocoding API JSON response |
| `expiresAt`    | Number | Unix timestamp (TTL, 30 days)           |

TTL is enabled on `expiresAt`.

---

## Project Structure

```
src/
 ├── Function.cs
 ├── GeocodeCacheRepository.cs
 ├── GoogleGeocodeClient.cs
 ├── AddressToCoordinatesLambda.csproj
 ├── aws-lambda-tools-defaults.json
 ├── payload.json
 └── README.md
```

### Responsibilities

#### `Function.cs`

* Lambda entry point
* Request validation
* Orchestrates cache lookup and Google API calls
* Handles HTTP responses

#### `GeocodeCacheRepository.cs`

* Encapsulates all DynamoDB access
* Implements cache read/write logic
* TTL handling

#### `GoogleGeocodeClient.cs`

* Encapsulates Google Geocoding API integration
* Responsible for external HTTP calls

This structure allows **easy extension**, testing, and maintenance.

---

## Configuration & Secrets

Secrets and configuration are **never hard‑coded**.

### Environment Variables

| Variable              | Description              |
| --------------------- | ------------------------ |
| `GOOGLE_MAPS_API_KEY` | Google Geocoding API key |

AWS credentials are provided via IAM Role attached to the Lambda function.

---

## Logging & Observability

* Uses **CloudWatch Logs** via `ILambdaContext.Logger`
* Logs clearly indicate:

  * Cache hits
  * Cache misses
  * External API calls

This allows easy verification of cache behavior during runtime.

---

## Error Handling

* `400 Bad Request` → Missing or invalid address
* `404 Not Found` → Address not resolved by Google
* `502 Bad Gateway` → Google API failure
* Defensive null checks throughout the pipeline

---

## How to Run & Test

1. Deploy Lambda using AWS tooling
2. Configure environment variable `GOOGLE_MAPS_API_KEY`
3. Call the endpoint with a U.S. address
4. Call the same address again to verify cache hit
5. Observe logs in CloudWatch

---

## Enterprise Readiness Highlights

✔ Separation of concerns
✔ Cache-aside pattern
✔ External API isolation
✔ TTL-based caching
✔ No hard-coded secrets
✔ Clean, readable code
✔ Easily extensible architecture

---

## Future Improvements

* Add unit tests for repository and client layers
* Add structured logging (correlation IDs)
* Support additional geocoding providers
* Introduce API rate limiting

---

## Author

Built as a technical exercise showcasing **modern .NET, AWS serverless design, and enterprise-grade best practices**.
