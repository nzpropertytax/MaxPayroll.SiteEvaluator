/**
 * Site Evaluator Map Component
 * Using Leaflet.js with LINZ basemap tiles
 */
const SiteEvaluatorMap = (function () {
    let map = null;
    let markers = [];
    let propertyBoundary = null;
    let boreholeMarkers = [];

    /**
     * Initialize the map
     * @param {string} containerId - ID of the container element
     * @param {Object} options - Map options
     */
    function init(containerId, options = {}) {
        const container = document.getElementById(containerId);
        if (!container) {
            console.error('Map container not found:', containerId);
            return null;
        }

        // Default options
        const defaults = {
            center: [-43.5321, 172.6362], // Christchurch
            zoom: 13,
            minZoom: 5,
            maxZoom: 19
        };

        const config = { ...defaults, ...options };

        // Create map
        map = L.map(containerId, {
            center: config.center,
            zoom: config.zoom,
            minZoom: config.minZoom,
            maxZoom: config.maxZoom
        });

        // Add LINZ basemap (requires API key for production)
        // Using OpenStreetMap as fallback
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
            maxZoom: 19
        }).addTo(map);

        // Optional: Add LINZ aerial imagery layer
        // Requires LINZ Data Service API key
        if (config.linzApiKey) {
            L.tileLayer('https://tiles-{s}.data-cdn.linz.govt.nz/services;key=' + config.linzApiKey + '/tiles/v4/layer=set=4702/EPSG:3857/{z}/{x}/{y}.png', {
                attribution: '&copy; <a href="https://www.linz.govt.nz/">LINZ</a>',
                subdomains: ['a', 'b', 'c', 'd'],
                maxZoom: 19
            });
        }

        return map;
    }

    /**
     * Set the map center
     */
    function setCenter(lat, lng, zoom = null) {
        if (!map) return;
        
        if (zoom) {
            map.setView([lat, lng], zoom);
        } else {
            map.setView([lat, lng]);
        }
    }

    /**
     * Add a property marker
     */
    function addPropertyMarker(lat, lng, options = {}) {
        if (!map) return null;

        const defaults = {
            icon: createPropertyIcon(),
            draggable: false
        };

        const config = { ...defaults, ...options };
        const marker = L.marker([lat, lng], config).addTo(map);

        if (options.popup) {
            marker.bindPopup(options.popup);
        }

        markers.push(marker);
        return marker;
    }

    /**
     * Add borehole markers
     */
    function addBoreholeMarkers(boreholes) {
        if (!map) return;

        clearBoreholeMarkers();

        boreholes.forEach(bh => {
            const marker = L.circleMarker([bh.latitude, bh.longitude], {
                radius: 6,
                fillColor: '#3498db',
                color: '#2980b9',
                weight: 2,
                opacity: 1,
                fillOpacity: 0.8
            }).addTo(map);

            marker.bindPopup(`
                <div class="map-popup">
                    <strong>Borehole</strong><br>
                    <small>ID: ${bh.id || 'N/A'}</small><br>
                    <small>Depth: ${bh.depth ? bh.depth + 'm' : 'N/A'}</small><br>
                    <small>Distance: ${bh.distance ? bh.distance.toFixed(0) + 'm' : 'N/A'}</small>
                </div>
            `);

            boreholeMarkers.push(marker);
        });
    }

    /**
     * Add CPT markers
     */
    function addCptMarkers(cpts) {
        if (!map) return;

        cpts.forEach(cpt => {
            const marker = L.circleMarker([cpt.latitude, cpt.longitude], {
                radius: 6,
                fillColor: '#9b59b6',
                color: '#8e44ad',
                weight: 2,
                opacity: 1,
                fillOpacity: 0.8
            }).addTo(map);

            marker.bindPopup(`
                <div class="map-popup">
                    <strong>CPT</strong><br>
                    <small>ID: ${cpt.id || 'N/A'}</small><br>
                    <small>Depth: ${cpt.depth ? cpt.depth + 'm' : 'N/A'}</small><br>
                    <small>Distance: ${cpt.distance ? cpt.distance.toFixed(0) + 'm' : 'N/A'}</small>
                </div>
            `);

            boreholeMarkers.push(marker);
        });
    }

    /**
     * Draw property boundary
     */
    function drawPropertyBoundary(coordinates) {
        if (!map) return;

        clearPropertyBoundary();

        if (!coordinates || coordinates.length < 3) return;

        const latLngs = coordinates.map(c => [c.latitude, c.longitude]);

        propertyBoundary = L.polygon(latLngs, {
            color: '#e74c3c',
            fillColor: '#e74c3c',
            fillOpacity: 0.2,
            weight: 2
        }).addTo(map);

        // Fit map to boundary
        map.fitBounds(propertyBoundary.getBounds(), { padding: [50, 50] });
    }

    /**
     * Clear property boundary
     */
    function clearPropertyBoundary() {
        if (propertyBoundary) {
            map.removeLayer(propertyBoundary);
            propertyBoundary = null;
        }
    }

    /**
     * Clear borehole markers
     */
    function clearBoreholeMarkers() {
        boreholeMarkers.forEach(m => map.removeLayer(m));
        boreholeMarkers = [];
    }

    /**
     * Clear all markers
     */
    function clearMarkers() {
        markers.forEach(m => map.removeLayer(m));
        markers = [];
        clearBoreholeMarkers();
    }

    /**
     * Create property icon
     */
    function createPropertyIcon() {
        return L.divIcon({
            className: 'property-marker',
            html: '<i class="fa fa-map-marker-alt" style="color: #e74c3c; font-size: 24px;"></i>',
            iconSize: [24, 24],
            iconAnchor: [12, 24],
            popupAnchor: [0, -24]
        });
    }

    /**
     * Create borehole icon
     */
    function createBoreholeIcon() {
        return L.divIcon({
            className: 'borehole-marker',
            html: '<i class="fa fa-circle" style="color: #3498db; font-size: 10px;"></i>',
            iconSize: [10, 10],
            iconAnchor: [5, 5]
        });
    }

    /**
     * Enable click-to-search
     */
    function enableClickSearch(callback) {
        if (!map) return;

        map.on('click', function (e) {
            if (typeof callback === 'function') {
                callback(e.latlng.lat, e.latlng.lng);
            }
        });
    }

    /**
     * Add legend
     */
    function addLegend() {
        if (!map) return;

        const legend = L.control({ position: 'bottomright' });

        legend.onAdd = function () {
            const div = L.DomUtil.create('div', 'map-legend');
            div.innerHTML = `
                <div class="legend-title">Legend</div>
                <div class="legend-item">
                    <span class="legend-marker" style="background: #e74c3c;"></span>
                    Property
                </div>
                <div class="legend-item">
                    <span class="legend-marker" style="background: #3498db;"></span>
                    Borehole
                </div>
                <div class="legend-item">
                    <span class="legend-marker" style="background: #9b59b6;"></span>
                    CPT
                </div>
            `;
            return div;
        };

        legend.addTo(map);
    }

    /**
     * Resize map (call when container changes size)
     */
    function invalidateSize() {
        if (map) {
            map.invalidateSize();
        }
    }

    /**
     * Destroy map
     */
    function destroy() {
        if (map) {
            map.remove();
            map = null;
        }
        markers = [];
        boreholeMarkers = [];
        propertyBoundary = null;
    }

    // Public API
    return {
        init,
        setCenter,
        addPropertyMarker,
        addBoreholeMarkers,
        addCptMarkers,
        drawPropertyBoundary,
        clearPropertyBoundary,
        clearBoreholeMarkers,
        clearMarkers,
        enableClickSearch,
        addLegend,
        invalidateSize,
        destroy,
        getMap: () => map
    };
})();
