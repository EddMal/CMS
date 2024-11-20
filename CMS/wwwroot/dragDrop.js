// wwwroot/dragDrop.js
    console.log('Script dragDrop.js loaded')
    window.initializeSortable = function initializeSortable(containerSelector) {
        console.log('Initialize sortable');
        const container = document.querySelector(containerSelector);
        if (container) {
            console.log('container found');
            new Sortable(container, {
                handle: '.content-item-layout-grid-item', // Handle for dragging (optional)
                draggable: '.content-item-layout-grid-item', // The items that can be dragged
                animation: 150, // Smooth animation during drag
                onEnd: function (evt) {
                    // Trigger the OnDragEndAsync method in Blazor
                        await DotNet.invokeMethodAsync('CMS', 'OnDragEndAsync')
                        .then(data => {
                            console.log('Drag operation ended and handled in Blazor');
                        })
                        .catch(error => {
                            console.error('Error invoking OnDragEndAsync:', error);
                        });
                }
            });
        }
        else
        {
            console.log('container not found');
        }
    }
 