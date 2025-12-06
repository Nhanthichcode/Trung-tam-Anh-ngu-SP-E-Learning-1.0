// File: wwwroot/js/question-editor.js

var questionEditor = (function () {
    var _currentIndex = 0;
    var _containerId = '';

    // Hàm khởi tạo (nhận số lượng câu hỏi hiện tại và ID của khung chứa)
    function init(startIndex, containerId) {
        _currentIndex = startIndex;
        _containerId = containerId || 'SubQuestionsContainer';
    }

    // Hàm thêm dòng
    function addRow() {
        var container = document.getElementById(_containerId);

        // Template HTML (đã chuyển các biến @Model thành JS)
        var html = `
            <div class="card mb-3 sub-question-item shadow-sm border-0 bg-light border-start border-5 border-success">
                <div class="card-header bg-white fw-bold d-flex justify-content-between align-items-center">
                    <span>Câu hỏi mới</span>
                    <button type="button" class="btn btn-sm btn-danger" onclick="questionEditor.removeRow(this)">Xóa</button>
                </div>
                <div class="card-body">
                    <div class="form-group mb-2">
                        <label class="small text-muted fw-bold">Nội dung</label>
                        <input name="SubQuestions[${_currentIndex}].Content" class="form-control" placeholder="Nhập câu hỏi..." required />
                    </div>
                    <div class="row g-2">
                        <div class="col-6"><input name="SubQuestions[${_currentIndex}].OptionA" class="form-control form-control-sm" placeholder="A" required /></div>
                        <div class="col-6"><input name="SubQuestions[${_currentIndex}].OptionB" class="form-control form-control-sm" placeholder="B" required /></div>
                        <div class="col-6"><input name="SubQuestions[${_currentIndex}].OptionC" class="form-control form-control-sm" placeholder="C" required /></div>
                        <div class="col-6"><input name="SubQuestions[${_currentIndex}].OptionD" class="form-control form-control-sm" placeholder="D" required /></div>
                    </div>
                    <div class="mt-2 d-flex align-items-center">
                        <span class="me-2 fw-bold">Đáp án đúng:</span>
                        <select name="SubQuestions[${_currentIndex}].CorrectAnswer" class="form-select form-select-sm w-auto fw-bold text-success">
                            <option value="A">A</option>
                            <option value="B">B</option>
                            <option value="C">C</option>
                            <option value="D">D</option>
                        </select>
                    </div>
                    <div class="mt-2">
                         <input name="SubQuestions[${_currentIndex}].Explaination" class="form-control form-control-sm" placeholder="Giải thích..." />
                    </div>
                </div>
            </div>`;

        container.insertAdjacentHTML('beforeend', html);
        _currentIndex++;

        // Gọi lại hàm đánh số thứ tự để cập nhật tiêu đề "Câu hỏi số X"
        reIndex();
    }

    // Hàm xóa dòng
    function removeRow(btn) {
        if (confirm('Bạn có chắc muốn xóa câu hỏi này?')) {
            btn.closest('.card').remove();
            reIndex(); // Đánh lại số thứ tự ngay sau khi xóa
        }
    }

    // Hàm đánh lại chỉ mục (Re-index) để gửi về Server không bị lỗi
    function validateForm() {
        return reIndex();
    }

    // Hàm nội bộ: Đánh lại số thứ tự và name attribute
    function reIndex() {
        var items = document.querySelectorAll('.sub-question-item');

        if (items.length === 0) {
            // alert("Cần ít nhất 1 câu hỏi!"); // Bật cái này nếu bắt buộc
            // return false;
        }

        items.forEach((item, index) => {
            // 1. Cập nhật tiêu đề "Câu hỏi số X"
            var headerSpan = item.querySelector('.card-header span');
            if (headerSpan) headerSpan.innerText = "Câu hỏi số " + (index + 1);

            // 2. Cập nhật name="SubQuestions[index]..."
            var inputs = item.querySelectorAll('input, select, textarea');
            inputs.forEach(input => {
                if (input.name) {
                    // Thay thế số cũ bằng index mới: SubQuestions[99] -> SubQuestions[0]
                    input.name = input.name.replace(/SubQuestions\[\d+\]/, `SubQuestions[${index}]`);
                }
            });
        });
        return true;
    }

    // Public các hàm ra ngoài để View gọi
    return {
        init: init,
        addRow: addRow,
        removeRow: removeRow,
        validateForm: validateForm
    };
})();