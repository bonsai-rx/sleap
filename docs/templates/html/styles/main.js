$(function() {
    var createCodeHeader = function(text) {
        return $(
            '<div class="codeHeader">'+
            '    <span class="language">'+ text +'</span>'+
            '    <button type="button" class="action" aria-label="Copy code">'+
            '		<span class="icon"><span class="glyphicon glyphicon-duplicate" role="presentation"></span></span>'+
            '		<span>Copy</span>'+
            '		<div class="successful-copy-alert is-transparent" aria-hidden="true">'+
            '			<span class="icon is-size-large">'+
            '				<span class="glyphicon glyphicon-ok" role="presentation"></span>'+
            '			</span>'+
            '		</div>'+
            '	</button>'+
            '</div>'
        );
    }

    var setCopyAlert = function($element) {
        var copyAlert = $element.find(".successful-copy-alert");
        copyAlert.removeClass("is-transparent");
        setTimeout(function() {
            copyAlert.addClass("is-transparent");
        }, 2000);
    }

    $("div.workflow").each(function() {
        var workflowPath = undefined;
        $(this).find("img").attr("src", (_, val) => {
            workflowPath = val;
            return workflowPath.replace(/\.[^.]+$/, ".svg");
        });
        var $codeHeader = createCodeHeader("Workflow");
        $(this).first().before($codeHeader);
        $codeHeader.find("button").click(function() {
            var $button = $(this);
            fetch(workflowPath).then(req => req.text()).then(contents => {
                navigator.clipboard.writeText(contents);
                setCopyAlert($button);
            });
        });
    });

    $("code.hljs").each(function() {
        var $this = $(this);
        var language = /lang-(.+?)(\s|$)/.exec($this.attr("class"))[1].toUpperCase();
        if (language === 'CS' || language === 'CSHARP') {
            language = "C#";
        }
        if (language === 'JS') {
            language = "JavaScript";
        }
        var $codeHeader = createCodeHeader(language);
        var $codeElement = $this.closest("pre");
        $codeElement.before($codeHeader);
        $codeHeader.find("button").click(function() {
            navigator.clipboard.writeText($codeElement.text());
            setCopyAlert($(this));
        });
    });
});