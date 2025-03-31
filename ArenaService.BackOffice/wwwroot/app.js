// CSV 파일 다운로드 함수
function downloadFile(fileName, base64Content, contentType) {
    const link = document.createElement("a");
    link.href = `data:${contentType};base64,${base64Content}`;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
} 