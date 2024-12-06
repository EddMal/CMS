// wwwroot/dragDrop.js
     console.log("script loaded from dragDrop.js.");
        function showAlert() {
            console.log('LOG TEST');
        }

window.preventDragOverDefault = function (e) {
    if (e.preventDefault) {  // Check if preventDefault is a function
        e.preventDefault();
    }
    console.writeline("Drag over event handled.");



    // Select all elements that are draggable
  //  document.addEventListener('DOMContentLoaded', function () {
  //      console.log("DOM loaded from dragDrop.js.");



        // Select all draggable elements
 //       document.querySelectorAll('[draggable="true"]').forEach(function (draggableElement) {

 //           draggableElement.addEventListener('OnDragStart', function (e) {
 //               console.log('Drag started JS');
  //              this.classList.add('dragging');

                // Create a custom drag preview
  //              var dragPreview = document.createElement('div');
 //               dragPreview.style.width = this.offsetWidth + 'px'; // Set width equal to the dragged element
  //              dragPreview.style.height = this.offsetHeight + 'px'; // Set height equal to the dragged element
 //               dragPreview.style.backgroundColor = '#f0f0f0'; // Custom background color
 //               dragPreview.style.border = '2px dashed #333'; // Custom border for preview
  //              dragPreview.style.pointerEvents = 'none'; // Avoid interaction with preview
   //             dragPreview.style.opacity = '0.7'; // Set opacity for the preview
   //             dragPreview.innerText = "Dragging..."; // Optional text for preview

                // Set the custom preview image (using the clone)
   //             e.dataTransfer.setDragImage(dragPreview, 0, 0); // Set offset to be at the top-left corner

                // Optionally, apply other styles (e.g., reduce opacity) to the dragged element
    //            this.style.opacity = '0.5'; // Change the opacity of the dragged element itself
     //       });

      //      draggableElement.addEventListener('dragend', function () {
      //          console.log('Drag ended');
     //           this.classList.remove('dragging');
      //          this.style.opacity = '1'; // Reset opacity after drag ends
     //       });
    //    });
 //   });

};
