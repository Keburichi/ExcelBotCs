import antfu from '@antfu/eslint-config'

export default antfu({
  ignores: [
    '.codegenie/**',
    '.cursor/**',
    '.husky/**',
    'docs/**',
    '**/asyncapi.yaml',
  ],
})
