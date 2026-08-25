/**
 * Security utilities for Quadspace.
 * Handles email obfuscation and input sanitization to protect against bot scraping and injection attacks.
 */

/**
 * Decodes an obfuscated email address.
 * The email is encoded as base64 to deter simple bot scraping.
 */
export function decodeEmail() {
    // "info@strovelabs.com" encoded in base64
    const encoded = "aW5mb0BzdHJvdmVsYWJzLmNvbQ==";
    return atob(encoded);
}

/**
 * Initializes the footer contact link by decoding and injecting the email address.
 * Replaces a placeholder element with a properly obfuscated mailto link.
 */
export function initializeContactLink() {
    // Use a small delay to ensure DOM is fully ready after Blazor render
    const attemptInit = () => {
        const linkElement = document.getElementById("contact-link-placeholder");
        if (!linkElement) {
            // Element not found yet; schedule another attempt
            // This can happen if the layout hasn't fully rendered
            requestAnimationFrame(attemptInit);
            return;
        }

        const email = decodeEmail();
        const link = document.createElement("a");
        link.href = "mailto:" + email;
        link.className = "footer-link";
        link.textContent = email;

        linkElement.replaceWith(link);
    };

    attemptInit();
}

/**
 * Sanitizes user input (player names, etc.) to prevent injection attacks.
 * Allows only alphanumeric characters, spaces, hyphens, and apostrophes.
 * 
 * @param {string} input - The raw input string
 * @returns {string} - Sanitized input
 */
export function sanitizePlayerName(input) {
    if (!input || typeof input !== 'string') {
        return '';
    }

    // Only allow alphanumeric, spaces, hyphens, apostrophes, and periods
    // Remove any HTML/script characters
    return input
        .trim()
        .replace(/[^a-zA-Z0-9\s\-'.]/g, '')
        .slice(0, 50); // Max 50 characters as per server validation
}

/**
 * Validates that a player name meets basic requirements.
 * 
 * @param {string} name - The player name to validate
 * @returns {object} - { valid: boolean, error: string | null }
 */
export function validatePlayerName(name) {
    const sanitized = sanitizePlayerName(name);

    if (!sanitized || sanitized.length === 0) {
        return { valid: false, error: "Name must contain at least one letter or number." };
    }

    if (sanitized.length > 50) {
        return { valid: false, error: "Name must be 50 characters or fewer." };
    }

    return { valid: true, error: null };
}

// Initialize contact link immediately when this module is imported.
// The initializeContactLink function will retry via requestAnimationFrame if the DOM element isn't ready yet.
// This approach works whether the module is imported early or late in the page lifecycle.
initializeContactLink();

