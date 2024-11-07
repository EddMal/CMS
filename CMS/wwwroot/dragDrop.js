// wwwroot/dragDrop.js
window.preventDragOverDefault = function (e) {
    if (e.preventDefault) {  // Check if preventDefault is a function
        e.preventDefault();
    }
    console.log("Drag over event handled.");
};
