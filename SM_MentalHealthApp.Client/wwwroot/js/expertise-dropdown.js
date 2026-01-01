window.updateExpertiseDropdownPosition = (inputElementRef, dropdownElementRef) => {
    try {
        if (!inputElementRef || !dropdownElementRef) {
            return;
        }
        
        // Get the actual DOM elements from Blazor ElementReference
        const input = inputElementRef;
        const dropdown = dropdownElementRef;
        
        if (!input || !dropdown) {
            return;
        }
        
        // getBoundingClientRect() returns coordinates relative to viewport (perfect for fixed positioning)
        const inputRect = input.getBoundingClientRect();
        const viewportHeight = window.innerHeight;
        const viewportWidth = window.innerWidth;
        const dropdownHeight = 300; // max-height of dropdown
        
        // Calculate available space
        const spaceBelow = viewportHeight - inputRect.bottom;
        const spaceAbove = inputRect.top;
        
        let top, left, width;
        
        // Determine if we should show above or below
        if (spaceBelow >= dropdownHeight || spaceBelow > spaceAbove) {
            // Position below input - align with bottom of input
            top = inputRect.bottom + 2;
        } else {
            // Position above input - align with top of input minus dropdown height
            top = inputRect.top - dropdownHeight - 2;
        }
        
        // Ensure dropdown doesn't go off screen vertically
        if (top < 0) {
            top = 10; // Add small margin from top of viewport
        }
        if (top + dropdownHeight > viewportHeight) {
            top = viewportHeight - dropdownHeight - 10; // Keep 10px margin from bottom
        }
        
        // Position horizontally - align with input
        left = inputRect.left;
        width = inputRect.width;
        
        // Ensure dropdown doesn't go off screen horizontally
        if (left + width > viewportWidth) {
            left = viewportWidth - width - 10; // Keep 10px margin from right
        }
        if (left < 0) {
            left = 10; // Keep 10px margin from left
        }
        
        // Apply styles using fixed positioning (relative to viewport, no scroll offset needed)
        dropdown.style.position = 'fixed';
        dropdown.style.top = top + 'px';
        dropdown.style.left = left + 'px';
        dropdown.style.width = width + 'px';
        dropdown.style.zIndex = '10001';
    } catch (error) {
        console.error('Error updating dropdown position:', error);
    }
};

