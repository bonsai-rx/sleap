export default {
    createCodeContainer: function(path) {
        const wrap = document.createElement("pre");
        wrap.innerHTML =
            '<a class="btn border-0 code-action" href="#" title="Copy">'+
            '  <i class="bi bi-clipboard"></i>'+
            '</a>';
        const button = wrap.querySelector("a");
        button.addEventListener("click", (e) => {
            e.preventDefault();
            fetch(path).then(req => req.text()).then(contents => {
                navigator.clipboard.writeText(contents);
                this.setCopyAlert(button);
            });
        });
        return wrap;
    },
    setCopyAlert: function(element) {
        const copyIcon = element.querySelector("i");
        element.classList.add("link-success");
        copyIcon.classList.add("bi-check-lg");
        copyIcon.classList.remove("bi-clipboard");
        setTimeout(function() {
            copyIcon.classList.remove("bi-check-lg");
            copyIcon.classList.add("bi-clipboard");
            element.classList.remove("link-success");
        }, 1000);
    },
    renderElement: function(element) {
        element.classList.add("hljs");
        const img = element.querySelector("img");
        const workflowPath = img.src;
        img.src = workflowPath.replace(/\.[^.]+$/, ".svg");

        const wrap = this.createCodeContainer(workflowPath);
        const parent = element.parentElement;
        parent.insertBefore(wrap, element);
        wrap.appendChild(element);
    },
    init: async function() {
        const observer = new MutationObserver(() => {
            const theme = document.documentElement.getAttribute("data-bs-theme");
            const root = document.querySelector(':root');
            root.style.setProperty("color-scheme", theme);
        }).observe(document.documentElement, { attributes: true, attributeFilter: ['data-bs-theme'] })
        for (const element of document.getElementsByClassName("workflow")) {
            this.renderElement(element)
        }
    }
}