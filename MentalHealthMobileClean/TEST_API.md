# Testing the API

## Login Endpoint

The login endpoint requires **POST** method, not GET. Browsers use GET by default, which causes a 405 error.

### Correct Way to Test

#### Using curl:
```bash
curl -k -X POST https://192.168.86.25:5262/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"john@doe.com","password":"demo123"}'
```

#### Using Postman or similar:
- Method: **POST**
- URL: `https://192.168.86.25:5262/api/auth/login`
- Headers: `Content-Type: application/json`
- Body (raw JSON):
  ```json
  {
    "email": "john@doe.com",
    "password": "demo123"
  }
  ```

#### Using browser (for testing only):
You can't directly test POST endpoints in a browser by typing the URL. Use:
- Browser DevTools → Network tab → Make a fetch request
- Or use a browser extension like "REST Client"
- Or use the Swagger UI at: `https://192.168.86.25:5262/swagger/index.html`

### Why 405 Error?

- **405 = Method Not Allowed**
- The endpoint only accepts POST requests
- Browsers use GET when you type a URL
- The server correctly rejects GET requests to this endpoint

### Swagger UI

The easiest way to test the API is through Swagger:
- Navigate to: `https://192.168.86.25:5262/swagger/index.html`
- Find the `/api/auth/login` endpoint
- Click "Try it out"
- Enter email and password
- Click "Execute"

This will make a proper POST request with the correct headers and body.

