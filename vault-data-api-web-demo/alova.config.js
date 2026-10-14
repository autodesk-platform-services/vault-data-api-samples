export default {
    generator: [
      {
        input: 'public/Vault REST Public API.yml',
        output: 'src/api',
        global: 'Apis',
      }
    ],
    type: 'module',
    autoUpdate: true,
}