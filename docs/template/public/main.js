import WorkflowContainer from "./workflow.js"

export default {
    defaultTheme: 'auto',
    iconLinks: [{
        icon: 'github',
        href: 'https://github.com/bonsai-rx/sleap',
        title: 'GitHub'
    }],
    start: () => {
        WorkflowContainer.init();
    }
}