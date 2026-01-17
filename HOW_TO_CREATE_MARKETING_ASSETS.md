# How to Create Marketing Assets - Step-by-Step Guide

## 1. Converting Mermaid Diagrams to Images

### Option A: Using Mermaid Live Editor (Easiest)

1. **Go to**: https://mermaid.live
2. **Copy** the Mermaid code from `WORKFLOW_DIAGRAM.md` (the code between the ```mermaid blocks)
3. **Paste** it into the left panel of Mermaid Live Editor
4. **Wait** for it to render on the right
5. **Click** the "Actions" menu (top right)
6. **Select** "Download PNG" or "Download SVG"
7. **Save** the file with a descriptive name (e.g., "service-request-lifecycle.png")

**Pro Tip**: For high-resolution images, use SVG format and convert to PNG at 300 DPI for print materials.

---

### Option B: Using GitHub (If you have a repo)

1. **Create** a new file in your repository (e.g., `diagrams.md`)
2. **Paste** the Mermaid code blocks from `WORKFLOW_DIAGRAM.md`
3. **Commit** and push to GitHub
4. **View** the file on GitHub - diagrams render automatically
5. **Right-click** on the rendered diagram
6. **Select** "Save image as..." or use a screenshot tool

---

### Option C: Using VS Code Extension

1. **Install** the "Markdown Preview Mermaid Support" extension in VS Code
2. **Open** `WORKFLOW_DIAGRAM.md` in VS Code
3. **Press** `Ctrl+Shift+V` (or `Cmd+Shift+V` on Mac) to open preview
4. **Right-click** on the diagram in preview
5. **Select** "Copy Image" or use a screenshot tool

---

### Option D: Using Command Line (For Developers)

1. **Install** Mermaid CLI:
   ```bash
   npm install -g @mermaid-js/mermaid-cli
   ```

2. **Create** a file with just the Mermaid code (e.g., `diagram.mmd`)

3. **Convert** to image:
   ```bash
   mmdc -i diagram.mmd -o diagram.png -w 1920 -H 1080
   ```

---

## 2. Creating Professional Diagrams in Draw.io

### Step-by-Step:

1. **Go to**: https://app.diagrams.net (or download the desktop app)

2. **Create New Diagram**:
   - Click "Create New Diagram"
   - Choose a template (Flowchart, UML, etc.) or start blank

3. **Recreate the Workflow**:
   - Use the shapes from the left panel
   - Drag and drop shapes for each step
   - Connect shapes with arrows
   - Add text labels

4. **Styling**:
   - Select shapes and use the format panel (right side)
   - Choose colors that match your brand
   - Add icons from the "More Shapes" menu

5. **Export**:
   - File → Export as → PNG (or SVG, PDF)
   - Set resolution to 300 DPI for print quality
   - Choose transparent background if needed

**Pro Tip**: Draw.io has templates for system architecture, flowcharts, and sequence diagrams that you can customize.

---

## 3. Recording the Demo Video

### Using OBS Studio (Free, Professional)

1. **Download**: https://obsproject.com

2. **Setup**:
   - Add "Display Capture" source (captures your screen)
   - Add "Audio Input Capture" for microphone
   - Add "Audio Output Capture" for system sounds (optional)

3. **Configure**:
   - Settings → Video → Base Canvas: 1920x1080
   - Output → Recording → Format: MP4
   - Output → Recording → Encoder: x264 (or hardware encoder if available)

4. **Prepare Your Screen**:
   - Open your application in a clean browser window
   - Close unnecessary tabs/apps
   - Use demo/staging environment with realistic data
   - Set browser zoom to 100%

5. **Record**:
   - Click "Start Recording"
   - Follow the script from `DEMO_VIDEO_SCRIPT.md`
   - Speak clearly into microphone
   - Click "Stop Recording" when done

6. **File Location**: Videos save to your Videos folder by default

---

### Using Camtasia (Paid, Easier Editing)

1. **Download**: https://www.techsmith.com/camtasia.html (free trial available)

2. **Record**:
   - Click "Record Screen"
   - Select screen area (full screen recommended)
   - Enable microphone
   - Click record button
   - Follow script

3. **Edit** (Camtasia has built-in editor):
   - Trim unwanted sections
   - Add callouts/text overlays
   - Add transitions
   - Add background music
   - Export when done

---

### Using QuickTime (Mac Only)

1. **Open** QuickTime Player
2. **File** → New Screen Recording
3. **Click** the record button
4. **Select** screen area or full screen
5. **Record** following the script
6. **Stop** recording (menu bar or Cmd+Ctrl+Esc)
7. **Save** the file

**Note**: You'll need separate software for editing (iMovie, Final Cut Pro, or DaVinci Resolve)

---

## 4. Editing the Demo Video

### Using DaVinci Resolve (Free, Professional)

1. **Download**: https://www.blackmagicdesign.com/products/davinciresolve

2. **Import**:
   - File → Import → Media
   - Select your recorded video
   - Drag to timeline

3. **Edit**:
   - **Trim**: Select clip, press "I" for in point, "O" for out point
   - **Add Text**: Effects → Titles → Drag to timeline
   - **Add Music**: Import audio file, drag below video track
   - **Adjust Volume**: Select audio, adjust in inspector panel

4. **Export**:
   - File → Deliver
   - Choose format (MP4 recommended)
   - Set resolution: 1920x1080
   - Click "Add to Render Queue" → "Render All"

---

### Using iMovie (Mac, Free)

1. **Open** iMovie
2. **Create New Project** → Movie
3. **Import** your recorded video
4. **Drag** video to timeline
5. **Edit**:
   - Trim: Drag edges of clips
   - Add titles: Click "Titles" button, drag to timeline
   - Add music: Click "Audio" button, drag to timeline
6. **Export**: Click share button → File → Save

---

### Using Windows Video Editor (Windows 10/11, Free)

1. **Open** Photos app
2. **Click** "Video Editor" (top right)
3. **New video project**
4. **Add** your recorded video
5. **Edit**:
   - Trim clips
   - Add text
   - Add music
6. **Export** video

---

## 5. Adding Voiceover

### Option A: Record While Recording Screen

- Use a good microphone (USB mic recommended)
- Record directly in OBS/Camtasia
- Speak clearly, follow the script
- Record in a quiet room

### Option B: Record Separately and Sync

1. **Record Audio**:
   - Use Audacity (free) or your phone's voice recorder
   - Read the script from `DEMO_VIDEO_SCRIPT.md`
   - Record in quiet environment

2. **Import to Video Editor**:
   - Import both video and audio
   - Sync them on timeline
   - Mute original video audio
   - Adjust audio levels

---

## 6. Creating Presentation Slides

### Using PowerPoint/Google Slides

1. **Create New Presentation**

2. **Slide 1 - Title**:
   - Product name and tagline
   - Your logo

3. **Slide 2 - Problem**:
   - "Service management is complex..."
   - Use diagram from workflow

4. **Slide 3 - Solution**:
   - "Customer Care Portal solves this..."
   - Key features (3-4 bullet points)

5. **Slide 4 - Architecture**:
   - Insert system architecture diagram (exported from Mermaid)
   - Add brief explanation

6. **Slide 5 - Key Features**:
   - Agentic AI
   - Real-time Communication
   - Unified Platform
   - (Use icons/images)

7. **Slide 6 - Workflow**:
   - Insert service request lifecycle diagram
   - Walk through process

8. **Slide 7 - Competitive Advantages**:
   - Comparison table or matrix
   - Highlight differentiators

9. **Slide 8 - Use Cases**:
   - Healthcare
   - Legal
   - Service Companies
   - (Add icons)

10. **Slide 9 - ROI/Benefits**:
    - Time savings
    - Cost reduction
    - Client satisfaction

11. **Slide 10 - Call to Action**:
    - Contact information
    - "Schedule a demo"

**Design Tips**:
- Use consistent color scheme (match your brand)
- Limit text per slide (6x6 rule: max 6 bullets, 6 words each)
- Use high-quality images
- Add animations sparingly

---

## 7. Creating One-Page Product Overview

### Using Canva (Easiest)

1. **Go to**: https://www.canva.com
2. **Create** → Custom Size → 8.5" x 11" (or A4)
3. **Design**:
   - Add header with logo
   - Section 1: Product name and tagline
   - Section 2: Key features (3-4 with icons)
   - Section 3: Benefits (bullet points)
   - Section 4: Use cases (brief)
   - Footer: Contact information
4. **Export** as PDF (for print) and PNG (for web)

### Using Word/Google Docs

1. **Create** new document
2. **Set** margins to 0.5" all around
3. **Add** sections similar to Canva
4. **Use** tables for layout
5. **Export** as PDF

---

## 8. Quick Reference: File Formats

- **Diagrams**: PNG (web), SVG (scalable), PDF (print)
- **Videos**: MP4 (H.264 codec, 1920x1080, 30fps)
- **Presentations**: PPTX (PowerPoint), PDF (universal)
- **Images**: PNG (with transparency), JPG (photos), SVG (logos)

---

## 9. Recommended Tools Summary

| Task | Free Option | Paid Option |
|------|-------------|-------------|
| **Diagrams** | Mermaid Live Editor, Draw.io | Lucidchart, Visio |
| **Screen Recording** | OBS Studio, QuickTime | Camtasia |
| **Video Editing** | DaVinci Resolve, iMovie | Adobe Premiere, Final Cut Pro |
| **Presentations** | Google Slides, LibreOffice | PowerPoint, Keynote |
| **Graphics** | Canva (free tier), GIMP | Adobe Creative Suite |
| **Audio Editing** | Audacity | Adobe Audition |

---

## 10. Quick Start Checklist

- [ ] Export all Mermaid diagrams as images
- [ ] Record demo video following script
- [ ] Edit video (add voiceover, transitions, text)
- [ ] Create presentation slides (10-15 slides)
- [ ] Design one-page product overview
- [ ] Create feature comparison sheet
- [ ] Prepare demo environment with realistic data
- [ ] Test all links and functionality before recording

---

## Need Help?

If you get stuck on any step:
1. Check the tool's official documentation
2. Search YouTube for tutorials (e.g., "How to use OBS Studio")
3. Use the free versions first to learn, then upgrade if needed
4. Start simple - you can always enhance later

**Remember**: Perfect is the enemy of done. Start with basic versions and iterate!
