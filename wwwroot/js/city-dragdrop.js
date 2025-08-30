document.addEventListener('DOMContentLoaded', () => {
    const tableBody = document.getElementById('table-body');
    let draggedItem = null;

    // CSS stillerini ekle
    const style = document.createElement('style');
    style.textContent = `
        .draggable-row {
            cursor: move;
            user-select: none;
        }
        .draggable-row.dragging {
            opacity: 0.5;
            background: #4B5563 !important;
        }
        .draggable-row.drag-over {
            border-top: 2px solid #3B82F6;
            background: #374151 !important;
        }
    `;
    document.head.appendChild(style);

    // Sürükleme başladığında
    tableBody.addEventListener('dragstart', (e) => {
        if (e.target.classList.contains('draggable-row')) {
            draggedItem = e.target;
            e.target.classList.add('dragging');
            e.dataTransfer.effectAllowed = 'move';
        }
    });

    // Sürükleme bittiğinde
    tableBody.addEventListener('dragend', (e) => {
        if (e.target.classList.contains('draggable-row')) {
            e.target.classList.remove('dragging');
            document.querySelectorAll('.drag-over').forEach(el => {
                el.classList.remove('drag-over');
            });
            draggedItem = null;
        }
    });

    // Sürükleme sırasında
    tableBody.addEventListener('dragover', (e) => {
        e.preventDefault();
        const targetRow = e.target.closest('.draggable-row');
        
        if (targetRow && targetRow !== draggedItem) {
            document.querySelectorAll('.drag-over').forEach(el => {
                el.classList.remove('drag-over');
            });
            targetRow.classList.add('drag-over');
        }
    });

    // Sürükleme bırakıldığında
    tableBody.addEventListener('drop', (e) => {
        e.preventDefault();
        const targetRow = e.target.closest('.draggable-row');
        
        if (targetRow && draggedItem && targetRow !== draggedItem) {
            targetRow.classList.remove('drag-over');
            
            // Sıralamayı güncelle
            const allRows = Array.from(tableBody.querySelectorAll('.draggable-row'));
            const draggedIndex = allRows.indexOf(draggedItem);
            const targetIndex = allRows.indexOf(targetRow);
            
            if (draggedIndex < targetIndex) {
                targetRow.parentNode.insertBefore(draggedItem, targetRow.nextSibling);
            } else {
                targetRow.parentNode.insertBefore(draggedItem, targetRow);
            }

            // Sunucuya yeni sıralamayı gönder
            const newOrder = Array.from(tableBody.querySelectorAll('.draggable-row')).map(row => ({
                cityId: parseInt(row.dataset.cityId),
                newOrder: Array.from(tableBody.querySelectorAll('.draggable-row')).indexOf(row)
            }));

            fetch('/City/UpdateOrder', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify(newOrder)
            })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    toastr.success('Sıralama güncellendi');
                } else {
                    toastr.error('Sıralama güncellenirken bir hata oluştu');
                }
            })
            .catch(error => {
                console.error('Hata:', error);
                toastr.error('Sıralama güncellenirken bir hata oluştu');
            });
        }
    });

    // Sürükleme ayrıldığında
    tableBody.addEventListener('dragleave', (e) => {
        const targetRow = e.target.closest('.draggable-row');
        if (targetRow) {
            targetRow.classList.remove('drag-over');
        }
    });
});