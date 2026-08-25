# Security Hardening Implementation - Quadspace

## Overview
This document summarizes the security improvements implemented to protect the Quadspace application against bot scraping, XSS attacks, and injection vulnerabilities.

## Changes Implemented

### 1. Email Obfuscation (Anti-Scraping)
**File**: `src/Quadspace.Client/wwwroot/js/security.js` (NEW)

The contact email address is now obfuscated to prevent bots from easily scraping it:
- Email `info@strovelabs.com` is encoded in base64 within the JavaScript file
- At runtime, JavaScript decodes the email and dynamically creates the mailto link
- Plain text email is never exposed in the HTML source code
- Bots performing simple HTML parsing will only see "loading..." placeholder

**Implementation Details**:
- `decodeEmail()` - Decodes the base64-encoded email
- `initializeContactLink()` - Replaces the placeholder with a proper mailto link
- Auto-initializes when the DOM is ready
- Gracefully handles missing placeholder element

### 2. Input Sanitization (Anti-XSS)
**Files Modified**:
- `src/Quadspace.Client/wwwroot/js/security.js` - Sanitization functions
- `src/Quadspace.Client/Pages/Game.razor.cs` - Client-side validation
- `src/Quadspace.Client/Pages/Game.razor` - Improved input field UX

**Player Name Sanitization**:
- `sanitizePlayerName(input)` - Removes any characters outside: `a-zA-Z0-9 -'.`
- Prevents HTML/JavaScript injection via player names
- Trims whitespace and enforces 50-character limit
- `validatePlayerName(name)` - Provides immediate client-side feedback

**Implementation Flow**:
1. User enters player name in game-over form
2. On form submission, `SubmitScoreAsync()` calls `SanitizePlayerNameAsync()`
3. JavaScript sanitization removes dangerous characters
4. Sanitized name is sent to server
5. Server-side `ScoreSubmission.TryNormalize()` validates again
6. Server HTML-encodes the name before storage

### 3. Output Encoding (Built-in via Blazor)
**File**: `src/Quadspace.Client/Pages/Home.razor`

Score names displayed on the high-score board are automatically HTML-encoded:
- Blazor's `@` expression syntax auto-encodes by default
- Any HTML entities or script tags in stored names are escaped and displayed as plain text
- This provides defense-in-depth against any injection that bypasses client validation

### 4. Server-Side Validation (Defense-in-Depth)
**File**: `src/Quadspace.Core/Scoring/ScoreSubmission.cs`

Comprehensive server-side validation:
- `TryNormalize()` enforces:
  - Name is required (non-empty after trim)
  - Name length ≤ 50 characters
  - Score is non-negative
- Used by the API endpoint at `POST /api/scores`
- Catches any bypassed client validation or direct API calls

### 5. Enhanced User Experience
**File**: `src/Quadspace.Client/Pages/Game.razor`

Improved input field:
- Better placeholder text: "ENTER NAME (A-Z, 0-9, spaces, hyphens, apostrophes)"
- Added `aria-label` for accessibility
- Error messages use `role="alert"` for screen readers
- Clearer feedback when submission fails

### 6. Footer Placeholder
**File**: `src/Quadspace.Client/Layout/MainLayout.razor`

- Contact email link replaced with `<span id="contact-link-placeholder">loading...</span>`
- JavaScript populates the actual link at runtime
- No sensitive information in the static HTML

## Security Layers

The implementation uses **defense-in-depth** with multiple layers:

1. **Bot Protection**: Email obfuscation prevents simple scraping
2. **Client-Side XSS Prevention**: Input sanitization removes dangerous characters
3. **Server-Side Validation**: TryNormalize() enforces strict rules
4. **Output Encoding**: Blazor auto-encodes display of user-generated content
5. **Clear Error Messages**: Helps users understand what input is allowed

## Testing Recommendations

1. **Test Email Obfuscation**:
   - Check browser DevTools - email should not appear in static HTML
   - Click the contact link to verify mailto: works correctly
   - Test with JavaScript disabled (graceful fallback: "loading...")

2. **Test Input Sanitization**:
   - Try submitting: `<script>alert('xss')</script>` - should be sanitized
   - Try submitting: `John's-Rule` - should be accepted
   - Try submitting: 51+ characters - should be truncated to 50
   - Try submitting: empty string - should show validation error

3. **Test Server Validation**:
   - Use curl/Postman to POST invalid data directly to `/api/scores`
   - Verify server rejects malformed requests

4. **Test High Score Display**:
   - Submit a name with special characters
   - Verify it displays safely on the high-score board without rendering HTML

## Security Best Practices Maintained

✅ No plaintext sensitive data in HTML source
✅ Input validation on both client and server
✅ Output encoding before display
✅ Clear separation of concerns (security.js)
✅ Graceful degradation (fallback if JavaScript fails)
✅ Accessibility considerations (aria-label, role="alert")
✅ Defense-in-depth approach (multiple validation layers)

## Future Enhancements

Consider for future releases:
- Rate limiting on score submissions to prevent abuse
- CAPTCHA for high-score entries (spam prevention)
- Content Security Policy (CSP) headers on the server
- Regular security audits and penetration testing
- Monitoring for suspicious score submissions
