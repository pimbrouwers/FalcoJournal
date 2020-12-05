const bulletEditor = document.getElementById('bullet-editor')
const bulletEditorInput = document.getElementById('bullet-editor-input')

window.onload = () => {
    bulletEditor.addEventListener('input', (e) => {
        let newContent = e.srcElement.innerHTML;
        bulletEditorInput.value = newContent.trim();
    })
}