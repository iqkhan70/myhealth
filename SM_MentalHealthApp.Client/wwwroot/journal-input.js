window.attachJournalInputListener = function (dotNetRef) {
    // Find the RadzenTextArea textarea element - try multiple selectors
    let textarea = document.querySelector('.rz-textarea textarea');
    
    // If not found, try finding by placeholder
    if (!textarea) {
        textarea = document.querySelector('textarea[placeholder*="How are you feeling"], textarea[placeholder*="Enter journal entry"]');
    }
    
    // If still not found, try finding any textarea in the journal form
    if (!textarea) {
        const journalForm = document.querySelector('.journal-form-card');
        if (journalForm) {
            textarea = journalForm.querySelector('textarea');
        }
    }
    
    if (textarea && !textarea.hasAttribute('data-journal-listener-attached')) {
        textarea.setAttribute('data-journal-listener-attached', 'true');
        
        const inputHandler = function() {
            dotNetRef.invokeMethodAsync('UpdateEntryText', this.value);
        };
        
        textarea.addEventListener('input', inputHandler);
        
        // Store the handler for cleanup
        textarea._journalInputHandler = inputHandler;
        textarea._journalDotNetRef = dotNetRef;
        return true;
    }
    return false;
};

window.detachJournalInputListener = function () {
    const textarea = document.querySelector('textarea[data-journal-listener-attached="true"]');
    if (textarea && textarea._journalInputHandler) {
        textarea.removeEventListener('input', textarea._journalInputHandler);
        textarea.removeAttribute('data-journal-listener-attached');
        delete textarea._journalInputHandler;
        delete textarea._journalDotNetRef;
    }
};

