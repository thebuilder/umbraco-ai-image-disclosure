# C2PA test fixtures

`signed-metadata.png` and `signed-composite.png` are synthetic credentials attached to this repository's package icon with ContentAuthenticity 0.90.16 and a C2PA test signing certificate. Their AI declarations are test data, not claims about the icon's origin.

- `signed-metadata.png` declares `trainedAlgorithmicMedia` in a namespaced `c2pa.metadata` assertion.
- `signed-composite.png` places that image as a `componentOf` ingredient using a hashed action reference.

Both are read and validated by the native SDK in the integration tests. Removal and manifest-scoped reference handling are covered separately with synthetic Reader JSON in `C2paCompositeTests.cs`.

The unsigned fixture is linked from the repository's `icon.png` at build time. No signing key is distributed with these fixtures.
