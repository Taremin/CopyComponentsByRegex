# CopyComponentsByRegex

[日本語版はこちら](./README.md)

## Overview

A Unity Editor extension that bulk-copies components matching a regular expression pattern to objects in identical hierarchical positions.

## Installation (VCC)

If you use VRChat Creator Companion (VCC), follow these steps:

1. Visit the [Listing page](https://taremin.github.io/CopyComponentsByRegex/)
2. Click "Add to VCC" to add the repository
3. Open your project in VCC and add `CopyComponentsByRegex` via "Manage Project"

## Installation (UPM)

1. Open `Window` -> `Package Manager` from Unity menu bar
2. Click the `+` button in the top left of Package Manager and select `Add package from git URL`
3. Paste `https://github.com/Taremin/CopyComponentsByRegex.git` and click `Add`

## Installation (ZIP)

Download [this repository's ZIP file](https://github.com/Taremin/CopyComponentsByRegex/archive/master.zip) and copy extracted contents into your assets folder.

### ZIP Installation Notes

Make sure to copy the `Editor` folder **as-is**.

This is due to Unity's specification that "scripts in the `Editor` folder are only valid in the editor and ignored during game execution."
(Reference: [Special Folder Names - Unity Manual](https://docs.unity3d.com/ja/2018.4/Manual/SpecialFolders.html))

If you only copy `*.cs` files from the `Editor` folder, errors will occur during runtime.

## How to Use

1. Select the source object in the Hierarchy
2. Right-click and select `Copy Components By Regex` from the context menu
3. In the `Copy Components By Regex` window, enter a regex pattern matching the components to copy
   (e.g., `Dynamic` to copy `Dynamic Bone` and `Dynamic Bone Collider`)
4. Click the `Copy` button
5. Select the destination object in the Hierarchy
6. Click the `Paste` button

### Checking Component Names

To check component names for writing regex patterns, expand the "Components" section.
Click the "Copy" button next to a component name to copy it to your clipboard.

## Notes

### References to Components Outside Copy Range

Object references within copied objects and components (like Dynamic Bone's `root`) are automatically replaced with destination objects and components.
However, references to components outside the copy range remain unchanged.

### Object Structure Matching

Structure similarity is determined by object names, so issues may occur with same-named child objects under the same parent.
Additionally, even if structures aren't identical, the tool tries to traverse matching child names, allowing copies even with added bones.

#### Name Mapping Rules

When source and destination have different object names (e.g., VRoid Studio's `J_Bip_C_Head` vs standard `Head`), use **Name Mapping Rules** to bridge naming differences.

Two types of rules are available:

1. **Regex Rules**: Transform names using arbitrary regex patterns
   - e.g., `J_Bip_C_(.+)` → `$1` (removes VRM prefix)

2. **Humanoid Bone Rules**: Dynamically match using Unity Humanoid rig mapping info
   - Both source and destination must be configured as Humanoid rigs
   - Supported groups: All, Head, Neck, Chest, Spine, Hips, Left Arm, Right Arm, Left Leg, Right Leg, Left Fingers, Right Fingers

See [Name Mapping Guide](./docs/name_mapping_guide.en.md) for details.

### Cloth Component Copy

For Cloth component copies between identical models with matching vertex counts, Constraints are simply copied (fast).
For different vertex counts or significantly different shapes, check `Cloth NNS (Nearest Neighbor)` (slower).
Note: Due to Unity's Cloth vertex coordinate issues during initialization, add a Cloth component to the destination beforehand.

## More Details

See https://taremin.github.io/2018/06/12/4-CopyComponentsByRegex_%E3%81%AE%E7%B0%A1%E5%8D%98%E3%81%AA%E4%BD%BF%E3%81%84%E6%96%B9%E3%81%A8%E8%AA%AC%E6%98%8E/ (Japanese) for more detailed explanations.

## Testing

### Requirements

- Unity 2019.2 or later

### Running Tests

1. Open `Window` -> `General` -> `Test Runner` in Unity Editor
2. Select the `EditMode` tab
3. Tests under `CopyComponentsByRegex.Tests` assembly will appear
4. Click `Run All` to execute tests

### Command Line Execution

```bash
Unity.exe -runTests -batchmode -projectPath <project path> -testPlatform EditMode -testResults results.xml
```

### Test Contents

- **KDTreeTests**: Tests KD-tree nearest neighbor search
- **CopyComponentsByRegexTests**: Tests basic data structures (TreeItem, ModificationEntry, etc.)
- **IntegrationTests**: Complex hierarchy integration tests
  - GetChildren, CopyWalkdown, MergeWalkdown core functionality
  - Regex matching, recursive hierarchy processing
- **NameMatcherTests**: Name matching functionality
  - Regex rule name transformations
  - HumanoidBone dynamic mapping
  - Child object search with replacement rules
- **PathUtilityTests**: Path utility functionality
  - Relative/absolute path retrieval
  - Package path resolution
- **LocalizationTests**: Internationalization functionality
  - Language switching
  - Key consistency across languages

## Language Settings

This tool supports both **English** and **Japanese**. You can change the language from the dropdown at the top of the editor window.

- **English**: UI displays in English
- **日本語**: UI displays in Japanese
- **System**: Automatically detects OS language

## License

[MIT](./LICENSE)

### Libraries Used

`CopyComponentsByRegex` uses modified code from:

- KDTree.cs - A Stark, September 2009. https://forum.unity.com/threads/point-nearest-neighbour-search-class.29923/
