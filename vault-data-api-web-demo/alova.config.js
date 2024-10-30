export default {
    generator: [
      {
        input: 'src/api/spec.json',
        output: 'src/api',
        global: 'Apis',
      }
    ],
    type: 'module',
    autoUpdate: true,
}