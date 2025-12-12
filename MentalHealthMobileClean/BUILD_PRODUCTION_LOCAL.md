# Build Production iOS App Locally (No Metro Needed)

## Why This Matters

A **development build** (`npx expo run:ios`) still needs Metro for JavaScript.
A **production build** bundles all JavaScript, so it works without Metro.

## Build Production Build Locally

### Step 1: Prebuild Native Code
```bash
cd MentalHealthMobileClean
npx expo prebuild
```

### Step 2: Build Release Version in Xcode

1. **Open Xcode:**
   ```bash
   open ios/MentalHealthMobile.xcworkspace
   ```

2. **In Xcode:**
   - Select **"MentalHealthMobile"** scheme (top left)
   - Change from **"Debug"** to **"Release"** (next to scheme)
   - Select your iPhone as target device
   - Product → Archive

3. **After Archive completes:**
   - Window → Organizer
   - Select your archive
   - Click **"Distribute App"**
   - Choose **"Ad Hoc"** (for testing) or **"Development"**
   - Follow the wizard to export IPA

4. **Install on iPhone:**
   - Transfer IPA to iPhone
   - Install via Xcode or Apple Configurator
   - Trust the developer certificate on iPhone

### Alternative: Command Line Build

```bash
cd MentalHealthMobileClean
npx expo prebuild

cd ios
xcodebuild -workspace MentalHealthMobile.xcworkspace \
  -scheme MentalHealthMobile \
  -configuration Release \
  -archivePath ./build/MentalHealthMobile.xcarchive \
  archive

# Then export the IPA from Xcode Organizer
```

## What This Gives You

- ✅ **No Metro needed** - All JavaScript is bundled
- ✅ **Works without your Mac** - App is fully standalone
- ✅ **Connects to staging server** - Uses `https://caseflowstage.store/api`
- ⚠️ **Still expires in 7 days** (free Apple ID limitation)

## After Building

The app will:
- Work completely independently
- Connect to your DigitalOcean server
- Not need Metro bundler
- Not need your Mac running

## Note

Even with a production build, if you use a **free Apple ID**, the app will expire after 7 days and need re-signing. For a permanent solution, get an Apple Developer account ($99/year).

