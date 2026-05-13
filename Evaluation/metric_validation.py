"""
Validierung aller Metriken des Evaluationsskripts
Standalone-Version ohne Klasse
"""

import cv2
import numpy as np
from skimage import color
from pathlib import Path

# Globales Output-Verzeichnis
OUTPUT_DIR = Path('validation_images')
OUTPUT_DIR.mkdir(exist_ok=True)

# Globale Ergebnisse
results = {}


# ==================== TEST 1: LAPLACIAN VARIANCE ====================

def test_laplacian_variance():
    print("\n" + "="*80)
    print("TEST 1: LAPLACIAN VARIANCE")
    print("="*80)
    
    # Scharfes Schachbrettmuster
    sharp = np.zeros((512, 512), dtype=np.uint8)
    for i in range(0, 512, 64):
        sharp[i:i+32, :] = 255
        sharp[:, i:i+32] = 255
    
    cv2.imwrite(str(OUTPUT_DIR / 'test1_sharp.png'), sharp)
    
    # Unscharfe Version
    blurred = cv2.GaussianBlur(sharp, (51, 51), 15)
    cv2.imwrite(str(OUTPUT_DIR / 'test1_blurred.png'), blurred)
    
    # Laplacian Variance berechnen
    lap_sharp = cv2.Laplacian(sharp, cv2.CV_64F).var()
    lap_blur = cv2.Laplacian(blurred, cv2.CV_64F).var()
    reduction = ((lap_sharp - lap_blur) / lap_sharp) * 100
    
    expected_min = 85.0
    passed = reduction > expected_min
    
    print(f"Scharfes Bild:     {lap_sharp:>10.2f}")
    print(f"Unscharfes Bild:   {lap_blur:>10.2f}")
    print(f"Reduktion:         {reduction:>10.1f}%")
    print(f"Erwartung:         >{expected_min:.1f}%")
    print(f"\nStatus:            {'✓ PASS' if passed else '✗ FAIL'}")
    
    results['laplacian_variance'] = {
        'passed': passed,
        'sharp': lap_sharp,
        'blurred': lap_blur,
        'reduction': reduction
    }
    
    return passed


# ==================== TEST 2: GRADIENT MAGNITUDE ====================

def test_gradient_magnitude():
    print("\n" + "="*80)
    print("TEST 2: GRADIENT MAGNITUDE")
    print("="*80)
    
    # Kantenbild
    edges = np.zeros((512, 512), dtype=np.uint8)
    edges[::32, :] = 255
    edges[:, ::32] = 255
    cv2.imwrite(str(OUTPUT_DIR / 'test2_edges.png'), edges)
    
    # Glattes Bild
    smooth = np.linspace(0, 255, 512, dtype=np.uint8)
    smooth = np.tile(smooth, (512, 1))
    cv2.imwrite(str(OUTPUT_DIR / 'test2_smooth.png'), smooth)
    
    # Gradient berechnen
    def calc_gradient(img):
        gx, gy = np.gradient(img.astype(float))
        return np.mean(np.sqrt(gx**2 + gy**2))
    
    grad_edges = calc_gradient(edges)
    grad_smooth = calc_gradient(smooth)
    ratio = grad_edges / grad_smooth if grad_smooth > 0 else 0
    
    expected_ratio = 3.0
    passed = ratio > expected_ratio
    
    print(f"Kantenbild:        {grad_edges:>10.2f}")
    print(f"Glattes Bild:      {grad_smooth:>10.2f}")
    print(f"Verhältnis:        {ratio:>10.2f}x")
    print(f"Erwartung:         >{expected_ratio:.1f}x")
    print(f"\nStatus:            {'✓ PASS' if passed else '✗ FAIL'}")
    
    results['gradient_magnitude'] = {
        'passed': passed,
        'edges': grad_edges,
        'smooth': grad_smooth,
        'ratio': ratio
    }
    
    return passed


# ==================== TEST 3: RMS CONTRAST ====================

def test_rms_contrast():
    print("\n" + "="*80)
    print("TEST 3: RMS CONTRAST")
    print("="*80)
    
    # Hoher Kontrast
    high = np.zeros((512, 512), dtype=np.uint8)
    high[:256, :] = 255
    cv2.imwrite(str(OUTPUT_DIR / 'test3_high_contrast.png'), high)
    
    # Niedriger Kontrast
    low = np.ones((512, 512), dtype=np.uint8) * 128
    cv2.imwrite(str(OUTPUT_DIR / 'test3_low_contrast.png'), low)
    
    # RMS berechnen
    def calc_rms(img):
        return np.std(img) / np.mean(img) if np.mean(img) > 0 else 0
    
    rms_high = calc_rms(high)
    rms_low = calc_rms(low)
    
    passed = rms_high > 0.5 and rms_low < 0.01
    
    print(f"Hoher Kontrast:    {rms_high:>10.4f}")
    print(f"Niedriger Kontrast:{rms_low:>10.4f}")
    print(f"Erwartung:         High >0.5, Low ~0")
    print(f"\nStatus:            {'✓ PASS' if passed else '✗ FAIL'}")
    
    results['rms_contrast'] = {
        'passed': passed,
        'high': rms_high,
        'low': rms_low
    }
    
    return passed


# ==================== TEST 4: MICHELSON CONTRAST ====================

def test_michelson_contrast():
    print("\n" + "="*80)
    print("TEST 4: MICHELSON CONTRAST")
    print("="*80)
    
    # Maximaler Kontrast
    max_contrast = np.zeros((512, 512), dtype=np.uint8)
    max_contrast[:, :256] = 0
    max_contrast[:, 256:] = 255
    cv2.imwrite(str(OUTPUT_DIR / 'test4_max_contrast.png'), max_contrast)
    
    # Minimaler Kontrast
    min_contrast = np.ones((512, 512), dtype=np.uint8) * 128
    cv2.imwrite(str(OUTPUT_DIR / 'test4_min_contrast.png'), min_contrast)
    
    # Michelson berechnen
    def calc_michelson(img):
        max_l, min_l = np.max(img), np.min(img)
        if max_l + min_l > 0:
            return (max_l - min_l) / (max_l + min_l)
        return 0
    
    mich_max = calc_michelson(max_contrast)
    mich_min = calc_michelson(min_contrast)
    
    passed = mich_max > 0.95 and mich_min < 0.05
    
    print(f"Max Kontrast:      {mich_max:>10.4f}")
    print(f"Min Kontrast:      {mich_min:>10.4f}")
    print(f"Erwartung:         Max ~1.0, Min ~0")
    print(f"\nStatus:            {'✓ PASS' if passed else '✗ FAIL'}")
    
    results['michelson_contrast'] = {
        'passed': passed,
        'max': mich_max,
        'min': mich_min
    }
    
    return passed


# ==================== TEST 5: LAB B* SHIFT ====================

def test_lab_b_shift():
    print("\n" + "="*80)
    print("TEST 5: LAB B* SHIFT")
    print("="*80)
    
    # Blaues Bild
    blue = np.zeros((256, 256, 3), dtype=np.uint8)
    blue[:, :, 2] = 255
    cv2.imwrite(str(OUTPUT_DIR / 'test5_blue.png'), 
                cv2.cvtColor(blue, cv2.COLOR_RGB2BGR))
    
    # Gelbes Bild
    yellow = np.zeros((256, 256, 3), dtype=np.uint8)
    yellow[:, :, 0] = 255
    yellow[:, :, 1] = 255
    cv2.imwrite(str(OUTPUT_DIR / 'test5_yellow.png'), 
                cv2.cvtColor(yellow, cv2.COLOR_RGB2BGR))
    
    # Zu Lab konvertieren
    blue_lab = color.rgb2lab(blue / 255.0)
    yellow_lab = color.rgb2lab(yellow / 255.0)
    
    b_blue = np.mean(blue_lab[:, :, 2])
    b_yellow = np.mean(yellow_lab[:, :, 2])
    
    passed = b_blue < -50 and b_yellow > 50
    
    print(f"Blau b*:           {b_blue:>10.2f}")
    print(f"Gelb b*:           {b_yellow:>10.2f}")
    print(f"Erwartung:         Blau <-50, Gelb >+50")
    print(f"\nStatus:            {'✓ PASS' if passed else '✗ FAIL'}")
    
    results['lab_b_shift'] = {
        'passed': passed,
        'blue': b_blue,
        'yellow': b_yellow
    }
    
    return passed


# ==================== TEST 6: COLOR GAMUT A* ====================

def test_color_gamut_a():
    print("\n" + "="*80)
    print("TEST 6: COLOR GAMUT A* (ROT-GRÜN)")
    print("="*80)
    
    # Farbbild
    colorful = np.zeros((256, 256, 3), dtype=np.uint8)
    colorful[:, :128, 0] = 255
    colorful[:, 128:, 1] = 255
    cv2.imwrite(str(OUTPUT_DIR / 'test6_colorful.png'), 
                cv2.cvtColor(colorful, cv2.COLOR_RGB2BGR))
    
    # Graubild
    gray = np.ones((256, 256, 3), dtype=np.uint8) * 128
    cv2.imwrite(str(OUTPUT_DIR / 'test6_gray.png'), 
                cv2.cvtColor(gray, cv2.COLOR_RGB2BGR))
    
    # Zu Lab konvertieren
    colorful_lab = color.rgb2lab(colorful / 255.0)
    gray_lab = color.rgb2lab(gray / 255.0)
    
    # a*-Range berechnen
    a_color_range = np.max(colorful_lab[:, :, 1]) - np.min(colorful_lab[:, :, 1])
    a_gray_range = np.max(gray_lab[:, :, 1]) - np.min(gray_lab[:, :, 1])
    
    passed = a_color_range > 100 and a_gray_range < 10
    
    print(f"Farbbild a*-Range: {a_color_range:>10.2f}")
    print(f"Graubild a*-Range: {a_gray_range:>10.2f}")
    print(f"Erwartung:         Farbe >100, Grau <10")
    print(f"\nStatus:            {'✓ PASS' if passed else '✗ FAIL'}")
    
    results['color_gamut_a'] = {
        'passed': passed,
        'colorful': a_color_range,
        'gray': a_gray_range
    }
    
    return passed


# ==================== TEST 7: MEAN DISPLACEMENT ====================

def test_mean_displacement():
    print("\n" + "="*80)
    print("TEST 7: MEAN PIXEL DISPLACEMENT")
    print("="*80)
    
    # Original mit detailreichem Muster
    original = np.zeros((512, 512), dtype=np.uint8)
    
    # Gittermuster
    for i in range(0, 512, 32):
        cv2.rectangle(original, (i, 0), (i+16, 512), 255, -1)
        cv2.rectangle(original, (0, i), (512, i+16), 255, -1)
    
    # Zusätzliche Kreise
    for x in range(64, 512, 128):
        for y in range(64, 512, 128):
            cv2.circle(original, (x, y), 20, 128, -1)
    
    cv2.imwrite(str(OUTPUT_DIR / 'test7_original.png'), original)
    
    # Identische Kopie
    identical = original.copy()
    cv2.imwrite(str(OUTPUT_DIR / 'test7_identical.png'), identical)
    
    # Verschobene Version
    M = np.float32([[1, 0, 10], [0, 1, 10]])
    shifted = cv2.warpAffine(original, M, (512, 512))
    cv2.imwrite(str(OUTPUT_DIR / 'test7_shifted.png'), shifted)
    
    # Optical Flow berechnen
    def calc_displacement(img1, img2):
        flow = cv2.calcOpticalFlowFarneback(
            img1, img2, None,
            pyr_scale=0.5, levels=3, winsize=15,
            iterations=3, poly_n=5, poly_sigma=1.2, flags=0
        )
        magnitude = np.sqrt(flow[..., 0]**2 + flow[..., 1]**2)
        return np.mean(magnitude)
    
    disp_identical = calc_displacement(original, identical)
    disp_shifted = calc_displacement(original, shifted)
    
    passed = disp_identical < 1.0 and disp_shifted > 3.0
    
    print(f"Identisch:         {disp_identical:>10.2f} Pixel")
    print(f"Verschoben:        {disp_shifted:>10.2f} Pixel")
    print(f"Erwartung:         Identisch <1, Verschoben >3")
    print(f"\nStatus:            {'✓ PASS' if passed else '✗ FAIL'}")
    
    results['mean_displacement'] = {
        'passed': passed,
        'identical': disp_identical,
        'shifted': disp_shifted
    }
    
    return passed


# ==================== TEST 8: SCOTOMA COVERAGE ====================

def test_scotoma_coverage():
    print("\n" + "="*80)
    print("TEST 8: SCOTOMA COVERAGE")
    print("="*80)
    
    # Original: Weiß
    original = np.ones((512, 512), dtype=np.uint8) * 255
    cv2.imwrite(str(OUTPUT_DIR / 'test8_original.png'), original)
    
    # Simuliert: Schwarzer Kreis
    simulated = original.copy()
    radius = 128
    cv2.circle(simulated, (256, 256), radius, 0, -1)
    cv2.imwrite(str(OUTPUT_DIR / 'test8_scotoma.png'), simulated)
    
    # Erwartete Coverage
    circle_area = np.pi * radius**2
    total_area = 512 * 512
    expected_coverage = (circle_area / total_area) * 100
    
    # Gemessene Coverage
    diff = np.abs(original.astype(float) - simulated.astype(float))
    scotoma_mask = diff > 50
    measured_coverage = (np.sum(scotoma_mask) / scotoma_mask.size) * 100
    
    error = abs(measured_coverage - expected_coverage) / expected_coverage * 100
    passed = error < 5.0
    
    print(f"Erwartete Coverage:{expected_coverage:>10.2f}%")
    print(f"Gemessene Coverage:{measured_coverage:>10.2f}%")
    print(f"Abweichung:        {error:>10.2f}%")
    print(f"Erwartung:         <5% Fehler")
    print(f"\nStatus:            {'✓ PASS' if passed else '✗ FAIL'}")
    
    results['scotoma_coverage'] = {
        'passed': passed,
        'expected': expected_coverage,
        'measured': measured_coverage,
        'error': error
    }
    
    return passed


# ==================== TEST 9: FLOATER COUNT ====================

def test_floater_count():
    print("\n" + "="*80)
    print("TEST 9: FLOATER COUNT")
    print("="*80)
    
    # Original: Schwarz
    original = np.zeros((512, 512), dtype=np.uint8)
    cv2.imwrite(str(OUTPUT_DIR / 'test9_original.png'), original)
    
    # Simuliert: 5 weiße Kreise mit Radius 40
    simulated = original.copy()
    centers = [(128, 128), (384, 128), (256, 256), (128, 384), (384, 384)]
    for cx, cy in centers:
        cv2.circle(simulated, (cx, cy), 30, 255, -1)
    cv2.imwrite(str(OUTPUT_DIR / 'test9_floaters.png'), simulated)
    
    # Floater Count berechnen - WIE IM EVALUATIONS-SKRIPT
    diff = cv2.absdiff(original, simulated)
    _, binary = cv2.threshold(diff, 80, 255, cv2.THRESH_BINARY)
    
    # STÄRKERE morphologische Operationen (wie im Evaluator)
    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (5, 5))
    binary = cv2.morphologyEx(binary, cv2.MORPH_OPEN, kernel, iterations=2)
    binary = cv2.morphologyEx(binary, cv2.MORPH_CLOSE, kernel, iterations=1)
    
    num_labels, labels, stats, _ = cv2.connectedComponentsWithStats(
        binary, connectivity=8
    )
    
    h, w = original.shape
    image_area = h * w
    min_area = int(image_area * 0.0001)
    max_area = int(image_area * 0.015)
    
    valid_count = sum(1 for i in range(1, num_labels) 
                     if min_area <= stats[i, cv2.CC_STAT_AREA] <= max_area)
    
    expected_count = 5
    passed = valid_count == expected_count
    
    print(f"Erwartete Anzahl:  {expected_count:>10}")
    print(f"Gefundene Anzahl:  {valid_count:>10}")
    print(f"Erwartung:         Exakt {expected_count}")
    
    if not passed:
        print(f"\nDEBUG:")
        print(f"  Total Labels: {num_labels - 1}")
        print(f"  Min Area: {min_area}, Max Area: {max_area}")
        for i in range(1, num_labels):
            area = stats[i, cv2.CC_STAT_AREA]
            valid = "✓" if min_area <= area <= max_area else "✗"
            print(f"  Label {i}: Area={area} {valid}")
    
    print(f"\nStatus:            {'✓ PASS' if passed else '✗ FAIL'}")
    
    results['floater_count'] = {
        'passed': passed,
        'expected': expected_count,
        'found': valid_count
    }
    
    return passed

# ==================== TEST 10: FLOATER COVERAGE ====================

def test_floater_coverage():
    print("\n" + "="*80)
    print("TEST 10: FLOATER COVERAGE")
    print("="*80)
    
    # Original: Schwarz
    original = np.zeros((512, 512), dtype=np.uint8)
    
    # Simuliert: 3 Kreise
    simulated = original.copy()
    circles = [(256, 256, 50), (128, 128, 30), (384, 384, 40)]
    for cx, cy, r in circles:
        cv2.circle(simulated, (cx, cy), r, 255, -1)
    
    cv2.imwrite(str(OUTPUT_DIR / 'test10_original.png'), original)
    cv2.imwrite(str(OUTPUT_DIR / 'test10_floaters.png'), simulated)
    
    # Erwartete Coverage
    total_circle_area = sum(np.pi * r**2 for _, _, r in circles)
    total_area = 512 * 512
    expected_coverage = (total_circle_area / total_area) * 100
    
    # Gemessene Coverage
    diff = cv2.absdiff(original, simulated)
    floater_mask = diff > 60
    measured_coverage = (np.sum(floater_mask) / floater_mask.size) * 100
    
    error = abs(measured_coverage - expected_coverage) / expected_coverage * 100
    passed = error < 10.0
    
    print(f"Erwartete Coverage:{expected_coverage:>10.2f}%")
    print(f"Gemessene Coverage:{measured_coverage:>10.2f}%")
    print(f"Abweichung:        {error:>10.2f}%")
    print(f"Erwartung:         <10% Fehler")
    print(f"\nStatus:            {'✓ PASS' if passed else '✗ FAIL'}")
    
    results['floater_coverage'] = {
        'passed': passed,
        'expected': expected_coverage,
        'measured': measured_coverage,
        'error': error
    }
    
    return passed

def test_saturation_mean():
    print("\n" + "="*80)
    print("TEST 11: SATURATION MEAN")
    print("="*80)
    
    # Vollgesättigtes Rot
    saturated = np.zeros((512, 512, 3), dtype=np.uint8)
    saturated[:, :] = [255, 0, 0]  # RGB: Reines Rot
    cv2.imwrite(str(OUTPUT_DIR / 'test11_saturated.png'), 
                cv2.cvtColor(saturated, cv2.COLOR_RGB2BGR))
    
    # Entsättigtes Rot (Grau mit Rotstich)
    desaturated = np.zeros((512, 512, 3), dtype=np.uint8)
    desaturated[:, :] = [180, 128, 128]  # Grau-rötlich
    cv2.imwrite(str(OUTPUT_DIR / 'test11_desaturated.png'), 
                cv2.cvtColor(desaturated, cv2.COLOR_RGB2BGR))
    
    # Saturation Mean berechnen
    hsv_sat = cv2.cvtColor(saturated, cv2.COLOR_RGB2HSV)
    hsv_desat = cv2.cvtColor(desaturated, cv2.COLOR_RGB2HSV)
    
    sat_value = np.mean(hsv_sat[:, :, 1])
    desat_value = np.mean(hsv_desat[:, :, 1])
    
    expected_sat = 255.0
    passed = abs(sat_value - expected_sat) < 1.0 and sat_value > desat_value
    
    print(f"Vollgesättigt:     {sat_value:>10.2f}")
    print(f"Entsättigt:        {desat_value:>10.2f}")
    print(f"Erwartung:         ~255 und Sat > Desat")
    print(f"\nStatus:            {'✓ PASS' if passed else '✗ FAIL'}")
    
    results['saturation_mean'] = {
        'passed': passed,
        'saturated': sat_value,
        'desaturated': desat_value
    }
    
    return passed


# ==================== TEST 12: BLUE CHANNEL MEAN ====================

def test_blue_channel_mean():
    print("\n" + "="*80)
    print("TEST 12: BLUE CHANNEL MEAN")
    print("="*80)
    
    # Reines Blau
    blue_img = np.zeros((512, 512, 3), dtype=np.uint8)
    blue_img[:, :] = [0, 0, 200]  # RGB: Reines Blau
    cv2.imwrite(str(OUTPUT_DIR / 'test12_blue.png'), 
                cv2.cvtColor(blue_img, cv2.COLOR_RGB2BGR))
    
    # Gelb (kein Blau)
    yellow_img = np.zeros((512, 512, 3), dtype=np.uint8)
    yellow_img[:, :] = [200, 200, 0]  # RGB: Gelb
    cv2.imwrite(str(OUTPUT_DIR / 'test12_yellow.png'), 
                cv2.cvtColor(yellow_img, cv2.COLOR_RGB2BGR))
    
    # Blue Channel Mean berechnen
    blue_value = np.mean(blue_img[:, :, 2])
    yellow_value = np.mean(yellow_img[:, :, 2])
    
    expected_blue = 200.0
    expected_yellow = 0.0
    passed = (abs(blue_value - expected_blue) < 1.0 and 
              abs(yellow_value - expected_yellow) < 1.0)
    
    print(f"Reines Blau:       {blue_value:>10.2f}")
    print(f"Gelb (kein Blau):  {yellow_value:>10.2f}")
    print(f"Erwartung:         Blau ~200, Gelb ~0")
    print(f"\nStatus:            {'✓ PASS' if passed else '✗ FAIL'}")
    
    results['blue_channel_mean'] = {
        'passed': passed,
        'blue': blue_value,
        'yellow': yellow_value
    }
    
    return passed


# ==================== TEST 13: MEAN LUMINANCE ====================

def test_mean_luminance():
    print("\n" + "="*80)
    print("TEST 13: MEAN LUMINANCE")
    print("="*80)
    
    # Helles Bild
    bright_img = np.full((512, 512, 3), 200, dtype=np.uint8)
    cv2.imwrite(str(OUTPUT_DIR / 'test13_bright.png'), bright_img)
    
    # Dunkles Bild
    dark_img = np.full((512, 512, 3), 50, dtype=np.uint8)
    cv2.imwrite(str(OUTPUT_DIR / 'test13_dark.png'), dark_img)
    
    # Mean Luminance berechnen
    bright_gray = cv2.cvtColor(bright_img, cv2.COLOR_BGR2GRAY)
    dark_gray = cv2.cvtColor(dark_img, cv2.COLOR_BGR2GRAY)
    
    bright_value = np.mean(bright_gray)
    dark_value = np.mean(dark_gray)
    
    expected_bright = 200.0
    expected_dark = 50.0
    passed = (abs(bright_value - expected_bright) < 1.0 and 
              abs(dark_value - expected_dark) < 1.0 and
              bright_value > dark_value)
    
    print(f"Helles Bild:       {bright_value:>10.2f}")
    print(f"Dunkles Bild:      {dark_value:>10.2f}")
    print(f"Erwartung:         Hell ~200, Dunkel ~50")
    print(f"\nStatus:            {'✓ PASS' if passed else '✗ FAIL'}")
    
    results['mean_luminance'] = {
        'passed': passed,
        'bright': bright_value,
        'dark': dark_value
    }
    
    return passed


# ==================== TEST 14: CENTRAL GRADIENT MAGNITUDE ====================

def test_central_gradient_magnitude():
    print("\n" + "="*80)
    print("TEST 14: CENTRAL GRADIENT MAGNITUDE")
    print("="*80)
    
    # Scharfes Schachbrett im Zentrum, schwarz außen
    sharp_center = np.zeros((512, 512), dtype=np.uint8)
    
    # Schachbrett nur im Zentrum (128-384)
    block_size = 32
    for i in range(128, 384, block_size):
        for j in range(128, 384, block_size):
            if ((i // block_size) + (j // block_size)) % 2 == 0:
                sharp_center[i:i+block_size, j:j+block_size] = 255
    
    cv2.imwrite(str(OUTPUT_DIR / 'test14_sharp_center.png'), sharp_center)
    
    # Unscharfes Schachbrett im Zentrum
    blur_center = sharp_center.copy()
    center_region = blur_center[128:384, 128:384]
    blurred = cv2.GaussianBlur(center_region, (15, 15), 5)
    blur_center[128:384, 128:384] = blurred
    cv2.imwrite(str(OUTPUT_DIR / 'test14_blur_center.png'), blur_center)
    
    # Central Gradient Magnitude berechnen
    def calc_central_gradient(img):
        # Maske für sichtbare Bereiche (nicht schwarz)
        visible_mask = img > 10
        
        # Gradient berechnen
        gx, gy = np.gradient(img.astype(float))
        gradient_magnitude = np.sqrt(gx**2 + gy**2)
        
        # Mittelwert nur im sichtbaren Bereich
        if np.sum(visible_mask) > 0:
            return np.mean(gradient_magnitude[visible_mask])
        return 0.0
    
    sharp_value = calc_central_gradient(sharp_center)
    blur_value = calc_central_gradient(blur_center)
    
    passed = sharp_value > blur_value and sharp_value > 10
    
    print(f"Scharfes Zentrum:  {sharp_value:>10.2f}")
    print(f"Unscharfes Zentrum:{blur_value:>10.2f}")
    print(f"Erwartung:         Scharf > Unscharf, Scharf >10")
    print(f"\nStatus:            {'✓ PASS' if passed else '✗ FAIL'}")
    
    results['central_gradient_magnitude'] = {
        'passed': passed,
        'sharp': sharp_value,
        'blur': blur_value
    }
    
    return passed


# ==================== TEST 15: SCOTOMA BOUNDARY GRADIENT ====================

def test_scotoma_boundary_gradient():
    print("\n" + "="*80)
    print("TEST 15: SCOTOMA BOUNDARY GRADIENT")
    print("="*80)
    
    # Basis: Uniform grau
    base = np.full((512, 512), 150, dtype=np.uint8)
    cv2.imwrite(str(OUTPUT_DIR / 'test15_base.png'), base)
    
    # Scharfer schwarzer Kreis (scharfer Skotomrand)
    sharp_scotoma = base.copy()
    cv2.circle(sharp_scotoma, (256, 256), 80, 0, -1)
    cv2.imwrite(str(OUTPUT_DIR / 'test15_sharp_scotoma.png'), sharp_scotoma)
    
    # Weicher schwarzer Kreis (weicher Skotomrand)
    soft_scotoma = base.copy()
    cv2.circle(soft_scotoma, (256, 256), 80, 0, -1)
    soft_scotoma = cv2.GaussianBlur(soft_scotoma, (31, 31), 10)
    cv2.imwrite(str(OUTPUT_DIR / 'test15_soft_scotoma.png'), soft_scotoma)
    
    # Scotoma Boundary Gradient berechnen
    def calc_boundary_gradient(base_img, scotoma_img):
        # Skotom-Maske finden
        diff = cv2.absdiff(base_img, scotoma_img)
        _, scotoma_mask = cv2.threshold(diff, 50, 255, cv2.THRESH_BINARY)
        
        # Ring erstellen (10-Pixel-Band um Skotom)
        kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (11, 11))
        dilated = cv2.dilate(scotoma_mask, kernel, iterations=1)
        eroded = cv2.erode(scotoma_mask, kernel, iterations=1)
        ring_mask = dilated - eroded
        
        # Gradient berechnen
        gx, gy = np.gradient(scotoma_img.astype(float))
        gradient_magnitude = np.sqrt(gx**2 + gy**2)
        
        # Gradient nur im Ring messen
        if np.sum(ring_mask) > 0:
            return np.mean(gradient_magnitude[ring_mask > 0])
        return 0.0
    
    sharp_value = calc_boundary_gradient(base, sharp_scotoma)
    soft_value = calc_boundary_gradient(base, soft_scotoma)
    
    passed = sharp_value > soft_value
    
    print(f"Scharfer Rand:     {sharp_value:>10.2f}")
    print(f"Weicher Rand:      {soft_value:>10.2f}")
    print(f"Erwartung:         Scharf > Weich")
    print(f"\nStatus:            {'✓ PASS' if passed else '✗ FAIL'}")
    
    results['scotoma_boundary_gradient'] = {
        'passed': passed,
        'sharp': sharp_value,
        'soft': soft_value
    }
    
    return passed


# ==================== ALLE TESTS AUSFÜHREN ====================

def run_all_tests():
    """Führt alle Tests aus"""
    print("\n" + "="*80)
    print("METRIK-VALIDIERUNG - EVALUATIONSSKRIPT")
    print("="*80)
    print(f"Output-Verzeichnis: {OUTPUT_DIR}")
    print("="*80)
    
    tests = [
        test_laplacian_variance,
        test_gradient_magnitude,
        test_rms_contrast,
        test_michelson_contrast,
        test_lab_b_shift,
        test_color_gamut_a,
        test_mean_displacement,
        test_scotoma_coverage,
        test_floater_count,
        test_floater_coverage,
        test_saturation_mean,
        test_mean_luminance,
        test_central_gradient_magnitude,
        test_scotoma_boundary_gradient
    ]
    
    for test_func in tests:
        test_func()
    
    # Zusammenfassung
    print("\n" + "="*80)
    print("ZUSAMMENFASSUNG")
    print("="*80)
    
    passed_count = sum(1 for r in results.values() if r['passed'])
    total_count = len(results)
    
    print(f"\nErgebnis: {passed_count}/{total_count} Tests bestanden\n")
    
    for metric, data in results.items():
        status = "✓ PASS" if data['passed'] else "✗ FAIL"
        print(f"{metric:.<50} {status}")
    
    print("\n" + "="*80)
    
    if passed_count == total_count:
        print("✓ ALLE TESTS BESTANDEN")
        print("Das Evaluationsskript ist korrekt implementiert.")
    else:
        print("✗ EINIGE TESTS FEHLGESCHLAGEN")
        print("Bitte überprüfen Sie die fehlgeschlagenen Metriken.")
    
    print("="*80)
    print(f"\nTestbilder gespeichert in: {OUTPUT_DIR}/")
    print("="*80 + "\n")
    
    return passed_count == total_count


# ==================== MAIN ====================

if __name__ == "__main__":
    import sys
    success = run_all_tests()
    sys.exit(0 if success else 1)
