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
        
        helpContent: {
            'step-1': {
                title: '?? Step 1: Enter Address',
                content: `
                    <h6>What to do:</h6>
                    <p>Enter the property address you want to evaluate. The system will search LINZ for matching properties.</p>
                    
                    <h6>Tips:</h6>
                    <ul>
                        <li>Type at least 3 characters to see suggestions</li>
                        <li>Use the full address including suburb and city</li>
                        <li>You can also search by title reference (e.g., CB45A/123)</li>
                    </ul>
                    
                    <h6>Alternative Search:</h6>
                    <p>Use the <strong>Title Reference</strong> tab to search by property title number if you have it.</p>
                `
            },
            'step-2': {
                title: '?? Step 2: Property Match',
                content: `
                    <h6>What to do:</h6>
                    <p>Select the correct property from the matches found. Review the legal description and area to confirm.</p>
                    
                    <h6>If no match found:</h6>
                    <ul>
                        <li>Check the address spelling</li>
                        <li>Try a broader search (street name only)</li>
                        <li>Use title reference search instead</li>
                    </ul>
                `
            },
            'step-3': {
                title: '??? Step 3: Zoning',
                content: `
                    <h6>What you'll see:</h6>
                    <p>Zoning information from the local council, including:</p>
                    <ul>
                        <li>Zone classification (Residential, Commercial, etc.)</li>
                        <li>Maximum building height</li>
                        <li>Site coverage limits</li>
                        <li>Setback requirements</li>
                    </ul>
                    
                    <h6>Data Source:</h6>
                    <p>Zoning data comes from council GIS systems. Always verify with the District Plan.</p>
                `
            },
            'step-4': {
                title: '?? Step 4: Natural Hazards',
                content: `
                    <h6>Hazards Assessed:</h6>
                    <ul>
                        <li><strong>Flooding</strong> - Flood zones and depths</li>
                        <li><strong>Liquefaction</strong> - TC1, TC2, TC3 categories</li>
                        <li><strong>Seismic</strong> - Fault proximity, ground shaking</li>
                        <li><strong>Landslide</strong> - Slope stability risks</li>
                    </ul>
                    
                    <h6>Important:</h6>
                    <p>Hazard data is indicative only. Site-specific assessments may be required.</p>
                `
            },
            'step-5': {
                title: '?? Step 5: Geotechnical',
                content: `
                    <h6>Data Retrieved:</h6>
                    <ul>
                        <li><strong>Nearby Boreholes</strong> - Historical drilling data</li>
                        <li><strong>CPT Results</strong> - Cone penetration tests</li>
                        <li><strong>Site Class</strong> - NZS 1170.5 classification</li>
                        <li><strong>Existing Reports</strong> - Previous geotech reports</li>
                    </ul>
                    
                    <h6>Source:</h6>
                    <p>Data from the New Zealand Geotechnical Database (NZGD).</p>
                `
            },
            'step-6': {
                title: '?? Step 6: Infrastructure',
                content: `
                    <h6>Services Checked:</h6>
                    <ul>
                        <li>Water supply availability</li>
                        <li>Wastewater/sewer connection</li>
                        <li>Stormwater drainage</li>
                        <li>Power supply</li>
                        <li>Fibre/broadband availability</li>
                    </ul>
                    
                    <h6>Note:</h6>
                    <p>Infrastructure availability doesn't guarantee connection. Contact utilities for confirmation.</p>
                `
            },
            'step-7': {
                title: '??? Step 7: Climate',
                content: `
                    <h6>Climate Data:</h6>
                    <ul>
                        <li><strong>Rainfall</strong> - Average and extreme rainfall</li>
                        <li><strong>Wind</strong> - Predominant direction and speed</li>
                        <li><strong>Temperature</strong> - Heating/cooling requirements</li>
                        <li><strong>Coastal</strong> - Sea level rise projections</li>
                    </ul>
                    
                    <h6>Source:</h6>
                    <p>Climate data from NIWA and council climate assessments.</p>
                `
            },
            'step-8': {
                title: '?? Step 8: Summary',
                content: `
                    <h6>Your Evaluation:</h6>
                    <p>Review all collected data and generate your report.</p>
                    
                    <h6>Available Reports:</h6>
                    <ul>
                        <li><strong>Summary Report</strong> - 1-page overview</li>
                        <li><strong>Full Report</strong> - Detailed assessment</li>
                        <li><strong>Geotech Brief</strong> - For engineers</li>
                    </ul>
                    
                    <h6>Data Gaps:</h6>
                    <p>Red items indicate missing data that may require further investigation.</p>
                `
            }
        },
        
        openHelp: function(section) {
            // Try the main website's help panel first
            if (window.openHelpPanel) {
                window.openHelpPanel('siteevaluator/user-guides/Site-Evaluation-Wizard-Guide', section);
                return;
            }
            
            // Fall back to built-in help panel
            this.showHelpPanel(section);
        },
        
        showHelpPanel: function(section) {
            var content = this.helpContent[section];
            if (!content) {
                content = {
                    title: 'Help',
                    content: '<p>Help content for this step is not available.</p>'
                };
            }
            
            // Check if panel already exists
            var panel = document.getElementById('seHelpPanel');
            if (!panel) {
                panel = this.createHelpPanel();
            }
            
            // Update content
            panel.querySelector('.help-panel-title').textContent = content.title;
            panel.querySelector('.help-panel-body').innerHTML = content.content;
            
            // Show panel
            panel.classList.add('show');
        },
        
        createHelpPanel: function() {
            var panel = document.createElement('div');
            panel.id = 'seHelpPanel';
            panel.className = 'se-help-panel';
            panel.innerHTML = `
                <div class="help-panel-header">
                    <span class="help-panel-title">Help</span>
                    <button type="button" class="help-panel-close" onclick="SiteEvaluatorWizard.closeHelpPanel()">
                        <i class="fa fa-times"></i>
                    </button>
                </div>
                <div class="help-panel-body"></div>
            `;
            document.body.appendChild(panel);
            
            // Add styles if not already present
            if (!document.getElementById('seHelpPanelStyles')) {
                var styles = document.createElement('style');
                styles.id = 'seHelpPanelStyles';
                styles.textContent = `
                    .se-help-panel {
                        position: fixed;
                        top: 0;
                        right: -400px;
                        width: 380px;
                        max-width: 90vw;
                        height: 100vh;
                        background: white;
                        box-shadow: -4px 0 20px rgba(0,0,0,0.15);
                        z-index: 9999;
                        transition: right 0.3s ease;
                        display: flex;
                        flex-direction: column;
                    }
                    .se-help-panel.show {
                        right: 0;
                    }
                    .help-panel-header {
                        display: flex;
                        justify-content: space-between;
                        align-items: center;
                        padding: 1rem 1.5rem;
                        background: #f8f9fa;
                        border-bottom: 1px solid #dee2e6;
                    }
                    .help-panel-title {
                        font-weight: 600;
                        font-size: 1.1rem;
                    }
                    .help-panel-close {
                        background: none;
                        border: none;
                        font-size: 1.2rem;
                        color: #6c757d;
                        cursor: pointer;
                        padding: 0.25rem 0.5rem;
                    }
                    .help-panel-close:hover {
                        color: #333;
                    }
                    .help-panel-body {
                        flex: 1;
                        overflow-y: auto;
                        padding: 1.5rem;
                    }
                    .help-panel-body h6 {
                        color: #0d6efd;
                        margin-top: 1rem;
                        margin-bottom: 0.5rem;
                    }
                    .help-panel-body h6:first-child {
                        margin-top: 0;
                    }
                    .help-panel-body ul {
                        padding-left: 1.25rem;
                    }
                    .help-panel-body li {
                        margin-bottom: 0.25rem;
                    }
                `;
                document.head.appendChild(styles);
            }
            
            return panel;
        },
        
        closeHelpPanel: function() {
            var panel = document.getElementById('seHelpPanel');
            if (panel) {
                panel.classList.remove('show');
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
