# Users API Endpoints - Technical Documentation

## Overview

The Users API provides secure endpoints for authenticated users to manage their profile information. All endpoints require valid JWT authentication and follow REST best practices.

---

## Security Features

### Authentication
- **JWT Bearer Token** required in `Authorization` header
- Token must contain user ID in one of these claims: `sub`, `NameIdentifier`, `userId`, or `id`
- Invalid or missing tokens return `401 Unauthorized`

### Data Protection
- **Input Sanitization**: All user input is sanitized to prevent XSS attacks
- **Email Normalization**: Emails are converted to lowercase for consistency
- **Whitespace Handling**: Excess whitespace is removed and normalized
- **SQL Injection Protection**: Entity Framework parameterized queries

### Validation
- **Model Validation**: Automatic validation using Data Annotations
- **Business Rules**: Email uniqueness check
- **Type Safety**: Strong typing with DTOs

---

## Endpoints

### 1. GET /api/users/me

Retrieves the profile information of the currently authenticated user.

#### Request

```http
GET /api/users/me HTTP/1.1
Host: localhost:5087
Authorization: Bearer <jwt-token>
Accept: application/json
```

#### Success Response (200 OK)

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fullName": "John Doe",
  "email": "john.doe@example.com",
  "phoneNumber": "+47 123 45 678",
  "createdAt": "2024-12-02T10:00:00Z",
  "updatedAt": "2024-12-02T11:30:00Z"
}
```

#### Error Responses

**401 Unauthorized** - Invalid or missing JWT token
```json
{
  "message": "Invalid authentication token",
  "detail": "Unable to identify user from provided token",
  "statusCode": 401,
  "timestamp": "2024-12-02T12:00:00Z"
}
```

**404 Not Found** - User does not exist
```json
{
  "message": "User not found",
  "detail": "The authenticated user does not exist in the system",
  "statusCode": 404,
  "timestamp": "2024-12-02T12:00:00Z"
}
```

**500 Internal Server Error** - Server error
```json
{
  "message": "An error occurred while retrieving user profile",
  "statusCode": 500,
  "timestamp": "2024-12-02T12:00:00Z"
}
```

---

### 2. PUT /api/users/me

Updates the profile information of the currently authenticated user.

#### Request

```http
PUT /api/users/me HTTP/1.1
Host: localhost:5087
Authorization: Bearer <jwt-token>
Content-Type: application/json

{
  "fullName": "Jane Smith",
  "email": "jane.smith@example.com",
  "phoneNumber": "+47 987 65 432"
}
```

#### Request Body Schema

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| `fullName` | string | Yes | 2-200 chars | User's full name |
| `email` | string | Yes | Valid email, max 320 chars | Email address |
| `phoneNumber` | string | Yes | Valid phone, 8-32 chars | Phone number |

#### Success Response (200 OK)

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fullName": "Trym Horlyk",
  "email": "trym.horlyk@gmail.com",
  "phoneNumber": "+47 987 65 432",
  "createdAt": "2024-12-02T10:00:00Z",
  "updatedAt": "2024-12-02T12:15:00Z"
}
```

#### Error Responses

**400 Bad Request** - Validation errors
```json
{
  "message": "Validation failed",
  "detail": "One or more fields contain invalid data",
  "statusCode": 400,
  "timestamp": "2024-12-02T12:00:00Z",
  "errors": {
    "Email": ["Invalid email format"],
    "FullName": ["Full name must be between 2 and 200 characters"],
    "PhoneNumber": ["Invalid phone number format"]
  }
}
```

**401 Unauthorized** - Invalid or missing JWT token
```json
{
  "message": "Invalid authentication token",
  "detail": "Unable to identify user from provided token",
  "statusCode": 401,
  "timestamp": "2024-12-02T12:00:00Z"
}
```

**404 Not Found** - User does not exist
```json
{
  "message": "User not found",
  "detail": "The authenticated user does not exist in the system",
  "statusCode": 404,
  "timestamp": "2024-12-02T12:00:00Z"
}
```

**409 Conflict** - Email already in use
```json
{
  "message": "Email already in use",
  "detail": "The provided email address is already registered to another user",
  "statusCode": 409,
  "timestamp": "2024-12-02T12:00:00Z"
}
```

**500 Internal Server Error** - Server error
```json
{
  "message": "Failed to update user profile",
  "detail": "A database error occurred",
  "statusCode": 500,
  "timestamp": "2024-12-02T12:00:00Z"
}
```

---

### 3. PUT /api/users/me/password

Changes the password for the currently authenticated user.

#### Request

```http
PUT /api/users/me/password HTTP/1.1
Host: localhost:5087
Authorization: Bearer <jwt-token>
Content-Type: application/json

{
  "currentPassword": "OldPassword123!",
  "newPassword": "NewSecurePassword456!",
  "confirmNewPassword": "NewSecurePassword456!"
}
```

#### Request Body Schema

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| `currentPassword` | string | Yes | - | User's current password |
| `newPassword` | string | Yes | Min 8 chars, must contain uppercase, lowercase, number, special char | New password |
| `confirmNewPassword` | string | Yes | Must match newPassword | Password confirmation |

#### Success Response (200 OK)

```json
{
  "message": "Password changed successfully",
  "timestamp": "2024-12-02T12:30:00Z"
}
```

#### Error Responses

**400 Bad Request** - Validation errors or incorrect current password
```json
{
  "message": "Incorrect password",
  "detail": "The current password provided is incorrect",
  "statusCode": 400,
  "timestamp": "2024-12-02T12:00:00Z"
}
```

Or validation errors:
```json
{
  "message": "Validation failed",
  "detail": "One or more fields contain invalid data",
  "statusCode": 400,
  "timestamp": "2024-12-02T12:00:00Z",
  "errors": {
    "NewPassword": ["Password must contain at least one uppercase letter, one lowercase letter, one number and one special character"],
    "ConfirmNewPassword": ["New password and confirmation do not match"]
  }
}
```

**401 Unauthorized** - Invalid or missing JWT token
```json
{
  "message": "Invalid authentication token",
  "detail": "Unable to identify user from provided token",
  "statusCode": 401,
  "timestamp": "2024-12-02T12:00:00Z"
}
```

**404 Not Found** - User does not exist
```json
{
  "message": "User not found",
  "detail": "The authenticated user does not exist in the system",
  "statusCode": 404,
  "timestamp": "2024-12-02T12:00:00Z"
}
```

**500 Internal Server Error** - Server error
```json
{
  "message": "An unexpected error occurred",
  "statusCode": 500,
  "timestamp": "2024-12-02T12:00:00Z"
}
```

**Password Requirements:**
- Minimum 8 characters
- At least one uppercase letter (A-Z)
- At least one lowercase letter (a-z)
- At least one digit (0-9)
- At least one special character (@$!%*?&)

---

## Data Processing

### Input Sanitization

All user input undergoes the following sanitization:

1. **Trim whitespace** from beginning and end
2. **Remove dangerous characters**: `< > & " ' / \`
3. **Normalize whitespace**: Multiple spaces reduced to single space
4. **Email normalization**: Convert to lowercase

### Example

**Input:**
```json
{
  "fullName": "  Jane   Smith  ",
  "email": "Jane.Smith@EXAMPLE.COM",
  "phoneNumber": " +47 123 45 678 "
}
```

**After Sanitization:**
```json
{
  "fullName": "Jane Smith",
  "email": "jane.smith@example.com",
  "phoneNumber": "+47 123 45 678"
}
```

---

## Testing

### Using cURL

**GET Request:**
```bash
curl -X GET "http://localhost:5087/api/users/me" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Accept: application/json"
```

**PUT Request (Update Profile):**
```bash
curl -X PUT "http://localhost:5087/api/users/me" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Jane Smith",
    "email": "jane.smith@example.com",
    "phoneNumber": "+47 987 65 432"
  }'
```

**PUT Request (Change Password):**
```bash
curl -X PUT "http://localhost:5087/api/users/me/password" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "currentPassword": "OldPassword123!",
    "newPassword": "NewSecurePassword456!",
    "confirmNewPassword": "NewSecurePassword456!"
  }'
```

### Using Svipp.Api.http

Open `src/Svipp.Api/Svipp.Api.http` in Visual Studio or Rider:

1. Set `@jwt_token` variable to your JWT token
2. Execute the requests directly in the IDE

---

## Logging

The controller logs the following events:

- **Info**: Successful profile retrievals and updates
- **Warning**: Failed token extraction, user not found, email conflicts
- **Error**: Database errors, unexpected exceptions

Log entries include user ID for traceability (when available).

---

## Best Practices Implemented

### Security
✅ JWT authentication required  
✅ Input sanitization against XSS  
✅ SQL injection protection via EF Core  
✅ Email uniqueness validation  
✅ Secure error messages (no sensitive data leakage)  

### Performance
✅ AsNoTracking for read operations  
✅ Efficient database queries  
✅ Minimal data transfer  

### Maintainability
✅ Comprehensive XML documentation  
✅ Structured error responses  
✅ Detailed logging  
✅ Clear separation of concerns  

### REST Compliance
✅ Proper HTTP status codes  
✅ Consistent response format  
✅ Content negotiation  
✅ Idempotent PUT operation  

---

## Future Enhancements

Potential improvements for future versions:

- [ ] Rate limiting per user
- [ ] Email verification on change
- [x] Password change endpoint
- [ ] Profile picture upload
- [ ] Audit log for profile changes
- [ ] Two-factor authentication
- [ ] Account deletion endpoint

---

## Support

For issues or questions, refer to:
- API documentation: `/swagger`
- Main README: `README.md`
- Database schema: `src/Svipp.Infrastructure/Migrations/`

