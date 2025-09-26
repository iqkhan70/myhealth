#!/usr/bin/env python3
"""
Create a professional PDF presentation from the Mental Health App documentation.
"""

import markdown
from reportlab.lib.pagesizes import letter, A4
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, PageBreak, Table, TableStyle
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.units import inch
from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_LEFT, TA_JUSTIFY
import re

def create_presentation():
    # Read the markdown file
    with open('MentalHealthApp_Presentation.md', 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Create PDF
    doc = SimpleDocTemplate(
        "MentalHealthApp_Presentation.pdf",
        pagesize=A4,
        rightMargin=72,
        leftMargin=72,
        topMargin=72,
        bottomMargin=18
    )
    
    # Get styles
    styles = getSampleStyleSheet()
    
    # Create custom styles
    title_style = ParagraphStyle(
        'CustomTitle',
        parent=styles['Heading1'],
        fontSize=24,
        spaceAfter=30,
        alignment=TA_CENTER,
        textColor=colors.darkblue
    )
    
    heading1_style = ParagraphStyle(
        'CustomHeading1',
        parent=styles['Heading1'],
        fontSize=18,
        spaceAfter=12,
        spaceBefore=20,
        textColor=colors.darkblue
    )
    
    heading2_style = ParagraphStyle(
        'CustomHeading2',
        parent=styles['Heading2'],
        fontSize=14,
        spaceAfter=8,
        spaceBefore=12,
        textColor=colors.darkgreen
    )
    
    bullet_style = ParagraphStyle(
        'Bullet',
        parent=styles['Normal'],
        fontSize=11,
        spaceAfter=6,
        leftIndent=20,
        bulletIndent=10
    )
    
    normal_style = ParagraphStyle(
        'Normal',
        parent=styles['Normal'],
        fontSize=11,
        spaceAfter=6,
        alignment=TA_JUSTIFY
    )
    
    # Parse content
    story = []
    lines = content.split('\n')
    
    i = 0
    while i < len(lines):
        line = lines[i].strip()
        
        if line.startswith('# '):
            # Main title
            title = line[2:].strip()
            story.append(Paragraph(title, title_style))
            story.append(Spacer(1, 20))
            
        elif line.startswith('## '):
            # Section heading
            heading = line[3:].strip()
            story.append(PageBreak())
            story.append(Paragraph(heading, heading1_style))
            
        elif line.startswith('### '):
            # Subsection heading
            heading = line[4:].strip()
            story.append(Paragraph(heading, heading2_style))
            
        elif line.startswith('- '):
            # Bullet point
            bullet_text = line[2:].strip()
            # Remove emoji and clean up
            bullet_text = re.sub(r'^[^\w\s]*\s*', '', bullet_text)
            story.append(Paragraph(f"• {bullet_text}", bullet_style))
            
        elif line.startswith('**') and line.endswith('**'):
            # Bold text
            bold_text = line[2:-2].strip()
            story.append(Paragraph(f"<b>{bold_text}</b>", normal_style))
            
        elif line and not line.startswith('---'):
            # Regular paragraph
            if line:
                story.append(Paragraph(line, normal_style))
        
        i += 1
    
    # Build PDF
    doc.build(story)
    print("PDF presentation created successfully: MentalHealthApp_Presentation.pdf")

if __name__ == "__main__":
    create_presentation()
