#!/usr/bin/env python3
"""
Create a professional PDF presentation from the Enhanced Mental Health App documentation.
"""

import markdown
from reportlab.lib.pagesizes import letter, A4
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, PageBreak, Table, TableStyle, Image
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.units import inch
from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_LEFT, TA_JUSTIFY
from reportlab.pdfgen import canvas
from reportlab.lib.pagesizes import A4
import re
import os

def create_enhanced_presentation():
    # Read the enhanced markdown file
    with open('Enhanced_Presentation.md', 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Create PDF
    doc = SimpleDocTemplate(
        "MentalHealthApp_Enhanced_Presentation.pdf",
        pagesize=A4,
        rightMargin=50,
        leftMargin=50,
        topMargin=50,
        bottomMargin=50
    )
    
    # Get styles
    styles = getSampleStyleSheet()
    
    # Create custom styles
    title_style = ParagraphStyle(
        'CustomTitle',
        parent=styles['Heading1'],
        fontSize=28,
        spaceAfter=30,
        alignment=TA_CENTER,
        textColor=colors.darkblue,
        fontName='Helvetica-Bold'
    )
    
    subtitle_style = ParagraphStyle(
        'Subtitle',
        parent=styles['Normal'],
        fontSize=16,
        spaceAfter=20,
        alignment=TA_CENTER,
        textColor=colors.darkgreen,
        fontName='Helvetica-Oblique'
    )
    
    heading1_style = ParagraphStyle(
        'CustomHeading1',
        parent=styles['Heading1'],
        fontSize=20,
        spaceAfter=15,
        spaceBefore=25,
        textColor=colors.darkblue,
        fontName='Helvetica-Bold'
    )
    
    heading2_style = ParagraphStyle(
        'CustomHeading2',
        parent=styles['Heading2'],
        fontSize=16,
        spaceAfter=10,
        spaceBefore=15,
        textColor=colors.darkgreen,
        fontName='Helvetica-Bold'
    )
    
    heading3_style = ParagraphStyle(
        'CustomHeading3',
        parent=styles['Heading3'],
        fontSize=14,
        spaceAfter=8,
        spaceBefore=12,
        textColor=colors.darkred,
        fontName='Helvetica-Bold'
    )
    
    bullet_style = ParagraphStyle(
        'Bullet',
        parent=styles['Normal'],
        fontSize=11,
        spaceAfter=4,
        leftIndent=20,
        bulletIndent=10,
        fontName='Helvetica'
    )
    
    normal_style = ParagraphStyle(
        'Normal',
        parent=styles['Normal'],
        fontSize=11,
        spaceAfter=6,
        alignment=TA_JUSTIFY,
        fontName='Helvetica'
    )
    
    code_style = ParagraphStyle(
        'Code',
        parent=styles['Code'],
        fontSize=9,
        spaceAfter=6,
        leftIndent=20,
        fontName='Courier',
        textColor=colors.darkblue,
        backColor=colors.lightgrey
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
            
        elif line.startswith('**') and line.endswith('**') and not line.startswith('###'):
            # Subtitle
            subtitle = line[2:-2].strip()
            story.append(Paragraph(subtitle, subtitle_style))
            
        elif line.startswith('## '):
            # Section heading
            heading = line[3:].strip()
            story.append(PageBreak())
            story.append(Paragraph(heading, heading1_style))
            
        elif line.startswith('### '):
            # Subsection heading
            heading = line[4:].strip()
            story.append(Paragraph(heading, heading2_style))
            
        elif line.startswith('#### '):
            # Sub-subsection heading
            heading = line[5:].strip()
            story.append(Paragraph(heading, heading3_style))
            
        elif line.startswith('- '):
            # Bullet point
            bullet_text = line[2:].strip()
            # Remove emoji and clean up
            bullet_text = re.sub(r'^[^\w\s]*\s*', '', bullet_text)
            story.append(Paragraph(f"• {bullet_text}", bullet_style))
            
        elif line.startswith('```'):
            # Code block
            i += 1
            code_lines = []
            while i < len(lines) and not lines[i].strip().startswith('```'):
                code_lines.append(lines[i])
                i += 1
            if code_lines:
                code_text = '\n'.join(code_lines)
                story.append(Paragraph(code_text, code_style))
            
        elif line.startswith('**') and ':' in line:
            # Bold label
            bold_text = line.strip()
            story.append(Paragraph(f"<b>{bold_text}</b>", normal_style))
            
        elif line and not line.startswith('---') and not line.startswith('*This presentation'):
            # Regular paragraph
            if line:
                # Handle special formatting
                if line.startswith('**') and line.endswith('**'):
                    line = f"<b>{line[2:-2]}</b>"
                elif line.startswith('✅'):
                    line = f"<b>✅</b> {line[2:]}"
                elif line.startswith('🚀'):
                    line = f"<b>🚀</b> {line[2:]}"
                elif line.startswith('🔧'):
                    line = f"<b>🔧</b> {line[2:]}"
                elif line.startswith('📊'):
                    line = f"<b>📊</b> {line[2:]}"
                elif line.startswith('🤖'):
                    line = f"<b>🤖</b> {line[2:]}"
                elif line.startswith('🗄️'):
                    line = f"<b>🗄️</b> {line[2:]}"
                elif line.startswith('🔒'):
                    line = f"<b>🔒</b> {line[2:]}"
                elif line.startswith('📈'):
                    line = f"<b>📈</b> {line[2:]}"
                elif line.startswith('🚀'):
                    line = f"<b>🚀</b> {line[2:]}"
                elif line.startswith('🔮'):
                    line = f"<b>🔮</b> {line[2:]}"
                elif line.startswith('💼'):
                    line = f"<b>💼</b> {line[2:]}"
                elif line.startswith('🛠️'):
                    line = f"<b>🛠️</b> {line[2:]}"
                elif line.startswith('📋'):
                    line = f"<b>📋</b> {line[2:]}"
                elif line.startswith('🎯'):
                    line = f"<b>🎯</b> {line[2:]}"
                elif line.startswith('📞'):
                    line = f"<b>📞</b> {line[2:]}"
                
                story.append(Paragraph(line, normal_style))
        
        i += 1
    
    # Add a final page with contact information
    story.append(PageBreak())
    story.append(Paragraph("Thank You", title_style))
    story.append(Spacer(1, 20))
    story.append(Paragraph("For your attention and consideration", subtitle_style))
    story.append(Spacer(1, 30))
    story.append(Paragraph("This presentation demonstrates a fully functional mental health application with AI-powered features, comprehensive user management, and intelligent medical data analysis capabilities.", normal_style))
    
    # Build PDF
    doc.build(story)
    print("Enhanced PDF presentation created successfully: MentalHealthApp_Enhanced_Presentation.pdf")

if __name__ == "__main__":
    create_enhanced_presentation()
