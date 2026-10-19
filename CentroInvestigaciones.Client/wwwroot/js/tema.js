window.centroTema = {
    leer: function () {
        return document.documentElement.getAttribute("data-tema") || localStorage.getItem("centro-tema") || "claro";
    },
    aplicar: function (tema) {
        var valor = tema || "claro";
        document.documentElement.setAttribute("data-tema", valor);
        localStorage.setItem("centro-tema", valor);
    }
};

window.centroVistos = {
    leer: function () {
        return localStorage.getItem("centro-avisos-vistos") || "[]";
    },
    guardar: function (texto) {
        localStorage.setItem("centro-avisos-vistos", texto || "[]");
    }
};

window.centroArchivo = {
    descargar: function (nombre, base64) {
        var binario = atob(base64);
        var bytes = new Uint8Array(binario.length);
        for (var i = 0; i < binario.length; i++) {
            bytes[i] = binario.charCodeAt(i);
        }
        var url = URL.createObjectURL(new Blob([bytes], { type: "application/pdf" }));
        var enlace = document.createElement("a");
        enlace.href = url;
        enlace.download = nombre || "hoja-de-vida.pdf";
        enlace.click();
        URL.revokeObjectURL(url);
    }
};
