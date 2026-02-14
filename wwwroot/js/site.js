// Max Site Evaluator - JavaScript

// Initialize Leaflet map if present
document.addEventListener('DOMContentLoaded', function() {
    const mapElement = document.getElementById('site-map');
    if (mapElement) {
        initializeMap(mapElement);
    }
});

function initializeMap(container) {
    // Default to Christchurch
    const defaultLat = -43.5321;
    const defaultLon = 172.6362;
    
    const map = L.map(container).setView([defaultLat, defaultLon], 13);
    
    // Use LINZ basemaps
    L.tileLayer('https://basemaps.linz.govt.nz/v1/tiles/aerial/WebMercatorQuad/{z}/{x}/{y}.webp?api=c01j0zvhc6nja95dcr7h7a42jha', {
        attribution: '&copy; <a href="https://www.linz.govt.nz/">LINZ</a>',
        maxZoom: 20
    }).addTo(map);
    
    // Click to search
    map.on('click', function(e) {
        const lat = e.latlng.lat.toFixed(6);
        const lon = e.latlng.lng.toFixed(6);
        window.location.href = `/Search?lat=${lat}&lon=${lon}`;
    });
    
    return map;
}

// Address autocomplete (placeholder - would integrate with LINZ API)
function setupAddressAutocomplete(inputElement) {
    // TODO: Implement address autocomplete using LINZ Address API
}
