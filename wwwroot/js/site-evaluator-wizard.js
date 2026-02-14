/**
 * Site Evaluator Wizard JavaScript
 * Extracted from inline scripts for proper separation of concerns
 */

(function () {
    'use strict';

    // ========================================
    // FlowStepper Integration
    // ========================================
    
    window.SiteEvaluatorWizard = {
        currentStep: 1,
        totalSteps: 8,
        
        /**
         * Initialize the wizard with configuration
         */
        init: function(config) {
            this.currentStep = config.currentStep || 1;
            this.totalSteps = config.totalSteps || 8;
            
            // Initialize FlowStepper if available
            if (window.FlowStepper) {
                this.stepper = new FlowStepper({
                    wizardId: 'site-evaluator-wizard',
                    currentStep: this.currentStep,
                    totalSteps: this.totalSteps
                });
            }
            
            this.initAddressAutocomplete();
            this.initPropertyMatchCards();
            this.initSearchTypeTabs();
            this.initIntendedUseForm();
        },
        
        // ========================================
        // Address Autocomplete
        // ========================================
        
        initAddressAutocomplete: function() {
            const addressInput = document.getElementById('address');
            const suggestionsContainer = document.getElementById('addressSuggestions');
            
            if (!addressInput || !suggestionsContainer) return;
            
            let debounceTimer;
            let suggestions = [];
            let currentIndex = -1;
            
            const self = this;
            
            addressInput.addEventListener('input', function() {
                clearTimeout(debounceTimer);
                debounceTimer = setTimeout(function() {
                    self.fetchAddressSuggestions(
                        addressInput.value.trim(), 
                        suggestionsContainer
                    );
                }, 300);
            });
            
            addressInput.addEventListener('keydown', function(e) {
                if (!suggestionsContainer.classList.contains('show')) return;
                
                const items = suggestionsContainer.querySelectorAll('.address-suggestion');
                
                switch(e.key) {
                    case 'ArrowDown':
                        e.preventDefault();
                        currentIndex = Math.min(currentIndex + 1, items.length - 1);
                        self.updateActiveItem(items, currentIndex);
                        break;
                    case 'ArrowUp':
                        e.preventDefault();
                        currentIndex = Math.max(currentIndex - 1, -1);
                        self.updateActiveItem(items, currentIndex);
                        break;
                    case 'Enter':
                        if (currentIndex >= 0 && suggestions[currentIndex]) {
                            e.preventDefault();
                            addressInput.value = suggestions[currentIndex].fullAddress;
                            suggestionsContainer.classList.remove('show');
                        }
                        break;
                    case 'Escape':
                        suggestionsContainer.classList.remove('show');
                        break;
                }
            });
            
            // Store suggestions for access in event handlers
            this.addressSuggestions = suggestions;
            this.currentSuggestionIndex = currentIndex;
            
            // Close suggestions when clicking outside
            document.addEventListener('click', function(e) {
                if (!addressInput.contains(e.target) && 
                    !suggestionsContainer.contains(e.target)) {
                    suggestionsContainer.classList.remove('show');
                }
            });
        },
        
        fetchAddressSuggestions: async function(query, container) {
            if (!query || query.length < 3) {
                container.classList.remove('show');
                return;
            }
            
            container.innerHTML = '<div class="p-3 text-muted"><i class="fa fa-spinner fa-spin"></i> Searching...</div>';
            container.classList.add('show');
            
            try {
                const response = await fetch('/api/siteevaluator/address/autocomplete?q=' + 
                    encodeURIComponent(query));
                    
                if (!response.ok) throw new Error('API error');
                
                const suggestions = await response.json();
                this.addressSuggestions = suggestions;
                this.renderAddressSuggestions(suggestions, container);
            } catch (error) {
                console.error('Autocomplete error:', error);
                container.innerHTML = '<div class="p-3 text-muted">Unable to load suggestions</div>';
            }
        },
        
        renderAddressSuggestions: function(suggestions, container) {
            if (suggestions.length === 0) {
                container.innerHTML = '<div class="p-3 text-muted">No addresses found</div>';
                return;
            }
            
            const self = this;
            
            container.innerHTML = suggestions.map(function(s, i) {
                return '<div class="address-suggestion" data-index="' + i + '">' +
                    '<div class="address-main">' + self.escapeHtml(s.fullAddress) + '</div>' +
                    '<div class="address-secondary">' + 
                    [s.suburb, s.city].filter(Boolean).join(', ') + 
                    '</div></div>';
            }).join('');
            
            container.querySelectorAll('.address-suggestion').forEach(function(el) {
                el.addEventListener('click', function() {
                    const index = parseInt(el.dataset.index);
                    if (suggestions[index]) {
                        document.getElementById('address').value = suggestions[index].fullAddress;
                        container.classList.remove('show');
                    }
                });
            });
            
            this.currentSuggestionIndex = -1;
        },
        
        updateActiveItem: function(items, currentIndex) {
            items.forEach(function(item, i) {
                item.classList.toggle('active', i === currentIndex);
                if (i === currentIndex) {
                    item.scrollIntoView({ block: 'nearest' });
                }
            });
        },
        
        escapeHtml: function(text) {
            if (!text) return '';
            const div = document.createElement('div');
            div.textContent = text;
            return div.innerHTML;
        },
        
        // ========================================
        // Property Match Cards
        // ========================================
        
        initPropertyMatchCards: function() {
            document.querySelectorAll('.property-match-card').forEach(function(card) {
                card.addEventListener('click', function() {
                    document.querySelectorAll('.property-match-card').forEach(function(c) {
                        c.classList.remove('selected');
                    });
                    this.classList.add('selected');
                    const radio = this.querySelector('input[type="radio"]');
                    if (radio) radio.checked = true;
                });
            });
        },
        
        // ========================================
        // Search Type Tabs
        // ========================================
        
        initSearchTypeTabs: function() {
            const searchTypeInput = document.getElementById('searchType');
            if (!searchTypeInput) return;
            
            const tabElements = document.querySelectorAll('[data-bs-toggle="tab"]');
            tabElements.forEach(function(tab) {
                tab.addEventListener('shown.bs.tab', function(e) {
                    const searchType = e.target.id.replace('-tab', '');
                    searchTypeInput.value = searchType;
                });
            });
        },
        
        // ========================================
        // Intended Use Form
        // ========================================
        
        initIntendedUseForm: function() {
            const useCategorySelect = document.getElementById('intendedUseCategory');
            const residentialParams = document.getElementById('residentialParams');
            const commercialParams = document.getElementById('commercialParams');
            const specificUseInput = document.getElementById('specificUse');
            
            if (!useCategorySelect) return;
            
            const specificUsePlaceholders = {
                'Residential': 'e.g., Single dwelling, Townhouses, Apartments...',
                'Commercial': 'e.g., Retail shop, Office, Restaurant, Hotel...',
                'Industrial': 'e.g., Warehouse, Manufacturing, Logistics...',
                'MixedUse': 'e.g., Retail with apartments above...',
                'Rural': 'e.g., Dairy farm, Vineyard, Lifestyle block...',
                'Community': 'e.g., School, Church, Community centre...',
                'OpenSpace': 'e.g., Park, Sports field, Reserve...',
                'Other': 'Describe the intended use...'
            };
            
            useCategorySelect.addEventListener('change', function() {
                const category = this.value;
                
                // Update placeholder
                if (specificUseInput) {
                    specificUseInput.placeholder = specificUsePlaceholders[category] || 
                        'Describe the intended use...';
                }
                
                // Show/hide relevant parameter sections
                if (residentialParams && commercialParams) {
                    if (category === 'Residential' || category === 'MixedUse') {
                        residentialParams.style.display = '';
                        commercialParams.style.display = 'none';
                    } else if (category === 'Commercial' || category === 'Industrial') {
                        residentialParams.style.display = 'none';
                        commercialParams.style.display = '';
                    } else {
                        residentialParams.style.display = 'none';
                        commercialParams.style.display = 'none';
                    }
                }
            });
        },
        
        // ========================================
        // Existing Evaluation Selection
        // ========================================
        
        selectExisting: function(id) {
            document.querySelectorAll('.existing-eval').forEach(function(c) {
                c.classList.remove('selected');
            });
            
            if (event && event.currentTarget) {
                event.currentTarget.classList.add('selected');
                var radio = event.currentTarget.querySelector('input[type="radio"]');
                if (radio) radio.checked = true;
            }
            
            // Uncheck "create new"
            var createNewCheck = document.getElementById('createNewCheck');
            var createNewInput = document.getElementById('createNewInput');
            if (createNewCheck) createNewCheck.checked = false;
            if (createNewInput) createNewInput.value = 'false';
            
            // Unselect LINZ matches
            document.querySelectorAll('.property-match-card:not(.existing-eval) input[type="radio"]')
                .forEach(function(r) { r.checked = false; });
            document.querySelectorAll('.property-match-card:not(.existing-eval)')
                .forEach(function(c) { c.classList.remove('selected'); });
        },
        
        toggleCreateNew: function(checkbox) {
            var createNewInput = document.getElementById('createNewInput');
            if (createNewInput) {
                createNewInput.value = checkbox.checked ? 'true' : 'false';
            }
            
            if (checkbox.checked) {
                // Unselect existing evaluations
                document.querySelectorAll('.existing-eval input[type="radio"]')
                    .forEach(function(r) { r.checked = false; });
                document.querySelectorAll('.existing-eval')
                    .forEach(function(c) { c.classList.remove('selected'); });
            }
        },
        
        // ========================================
        // Help Panel
        // ========================================
        
        openHelp: function(section) {
            if (window.openHelpPanel) {
                window.openHelpPanel('siteevaluator/user-guides/Site-Evaluation-Wizard-Guide', section);
            }
        }
    };
    
    // Make selection functions globally available for onclick handlers
    window.selectExisting = function(id) {
        SiteEvaluatorWizard.selectExisting(id);
    };
    
    window.toggleCreateNew = function(checkbox) {
        SiteEvaluatorWizard.toggleCreateNew(checkbox);
    };
    
})();
