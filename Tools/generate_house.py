from PIL import Image, ImageDraw

# 96x96 pixel house (3x3 tiles at 32ppu)
size = 96
img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
draw = ImageDraw.Draw(img)

# Walls - warm wood brown
wall_color = (180, 140, 90, 255)
draw.rectangle([8, 40, 88, 88], fill=wall_color)

# Roof - warm terracotta/red-brown
roof_color = (160, 80, 60, 255)
# Triangle roof
draw.polygon([(4, 42), (48, 8), (92, 42)], fill=roof_color)

# Roof outline
roof_outline = (120, 60, 40, 255)
draw.line([(4, 42), (48, 8), (92, 42)], fill=roof_outline, width=2)

# Door - darker wood
door_color = (120, 80, 50, 255)
draw.rectangle([38, 60, 58, 88], fill=door_color)

# Door handle
draw.ellipse([52, 72, 56, 76], fill=(200, 180, 100, 255))

# Window left - warm yellow glow
window_color = (255, 230, 150, 255)
draw.rectangle([14, 50, 30, 62], fill=window_color)
# Window frame
draw.rectangle([14, 50, 30, 62], outline=(100, 70, 40, 255), width=1)
draw.line([(22, 50), (22, 62)], fill=(100, 70, 40, 255), width=1)

# Window right
draw.rectangle([66, 50, 82, 62], fill=window_color)
draw.rectangle([66, 50, 82, 62], outline=(100, 70, 40, 255), width=1)
draw.line([(74, 50), (74, 62)], fill=(100, 70, 40, 255), width=1)

# Chimney
chimney_color = (140, 100, 70, 255)
draw.rectangle([68, 8, 78, 30], fill=chimney_color)

# Save
import os
out_dir = os.path.join(os.path.dirname(__file__), '..', 'Assets', 'Art', 'Sprites')
os.makedirs(out_dir, exist_ok=True)
out_path = os.path.join(out_dir, 'House.png')
img.save(out_path)
print(f'House sprite saved to {out_path}')
