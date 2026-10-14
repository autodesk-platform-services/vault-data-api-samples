import js from '@eslint/js';
import globals from 'globals';

export default [{
  files: ['dev/**/*.js'],
  languageOptions: { ecmaVersion: 'latest', sourceType: 'module', globals: globals.node },
  rules: js.configs.recommended.rules,
}];
