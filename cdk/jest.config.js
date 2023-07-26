module.exports = {
  testEnvironment: 'node',
  roots: ['<rootDir>/test'],
  testMatch: ['**/*.test.ts'],
  transform: {
    '^.+\\.tsx?$': 'ts-jest'
  }
};

// https://github.com/Zocdoc/frontend-common/blob/8d667b8794aae13016e6070d9df34a9dc17a40f1/jest.config.js#L13
process.env.AWS_SDK_LOAD_CONFIG = '1';
