import cv2
import numpy as np
from skimage import color
from skimage.metrics import structural_similarity as ssim
import json
from pathlib import Path

class VisionImpairmentEvaluator:
    """
    Universal-Evaluator für alle Sehprobleme
    """
    
    def __init__(self, original_path, simulated_path):
        self.orig_rgb = cv2.imread(original_path)
        self.sim_rgb = cv2.imread(simulated_path)
        
        if self.orig_rgb is None or self.sim_rgb is None:
            raise ValueError("Bilder konnten nicht geladen werden")
        
        # RGB konvertieren
        self.orig_rgb = cv2.cvtColor(self.orig_rgb, cv2.COLOR_BGR2RGB)
        self.sim_rgb = cv2.cvtColor(self.sim_rgb, cv2.COLOR_BGR2RGB)
        
        # Größen angleichen
        if self.orig_rgb.shape != self.sim_rgb.shape:
            self.sim_rgb = cv2.resize(self.sim_rgb, 
                                      (self.orig_rgb.shape[1], self.orig_rgb.shape[0]))
        
        # Graustufen
        self.orig_gray = cv2.cvtColor(self.orig_rgb, cv2.COLOR_RGB2GRAY)
        self.sim_gray = cv2.cvtColor(self.sim_rgb, cv2.COLOR_RGB2GRAY)
        
        # Lab-Farbraum
        self.orig_lab = color.rgb2lab(self.orig_rgb / 255.0)
        self.sim_lab = color.rgb2lab(self.sim_rgb / 255.0)
    
    def validate_pair(self):
        """Prüft, ob Bildpaar geeignet ist"""
        ssim_val = ssim(self.orig_gray, self.sim_gray)
        mse = np.mean((self.orig_gray.astype(float) - self.sim_gray.astype(float))**2)
        
        print("\n📋 BILDPAAR-VALIDIERUNG:")
        print(f"   SSIM: {ssim_val:.3f} (sollte >0.6 sein)")
        print(f"   MSE:  {mse:.2f} (sollte >50 sein)")
        
        if ssim_val < 0.6:
            print("   ⚠️  WARNUNG: SSIM zu niedrig (Kamera bewegt?)")
            return True
        if mse < 50:
            print("   ⚠️  WARNUNG: MSE zu niedrig (Filter aktiv?)")
            return True
        
        print("   ✓ Bildpaar geeignet")
        return True
    
    # ==================== BLUR METRIKEN ====================
    
    def laplacian_variance(self):
        """Laplacian Variance (Unschärfe-Metrik)"""
        lap_orig = cv2.Laplacian(self.orig_gray, cv2.CV_64F).var()
        lap_sim = cv2.Laplacian(self.sim_gray, cv2.CV_64F).var()
        
        return {
            'original': lap_orig,
            'simulated': lap_sim,
            'direction': 'decreased' if lap_sim < lap_orig else 'increased',
            'change_percent': ((lap_orig - lap_sim) / lap_orig) * 100 if lap_orig > 0 else 0
        }
    
    def gradient_magnitude(self):
        """Gradient Magnitude (Kantenschärfe)"""
        gx_o, gy_o = np.gradient(self.orig_gray.astype(float))
        gx_s, gy_s = np.gradient(self.sim_gray.astype(float))
        
        grad_orig = np.mean(np.sqrt(gx_o**2 + gy_o**2))
        grad_sim = np.mean(np.sqrt(gx_s**2 + gy_s**2))
        
        return {
            'original': grad_orig,
            'simulated': grad_sim,
            'direction': 'decreased' if grad_sim < grad_orig else 'increased',
            'change_percent': ((grad_orig - grad_sim) / grad_orig) * 100 if grad_orig > 0 else 0
        }
    
    # ==================== KONTRAST METRIKEN ====================
    
    def rms_contrast(self):
        """RMS Contrast"""
        rms_orig = np.std(self.orig_gray) / np.mean(self.orig_gray) if np.mean(self.orig_gray) > 0 else 0
        rms_sim = np.std(self.sim_gray) / np.mean(self.sim_gray) if np.mean(self.sim_gray) > 0 else 0
        
        return {
            'original': rms_orig,
            'simulated': rms_sim,
            'direction': 'decreased' if rms_sim < rms_orig else 'increased',
            'change_percent': ((rms_orig - rms_sim) / rms_orig) * 100 if rms_orig > 0 else 0
        }
    
    def michelson_contrast(self):
        """Michelson Contrast"""
        max_o, min_o = np.max(self.orig_gray), np.min(self.orig_gray)
        max_s, min_s = np.max(self.sim_gray), np.min(self.sim_gray)
        
        mich_orig = (max_o - min_o) / (max_o + min_o) if (max_o + min_o) > 0 else 0
        mich_sim = (max_s - min_s) / (max_s + min_s) if (max_s + min_s) > 0 else 0
        
        return {
            'original': mich_orig,
            'simulated': mich_sim,
            'direction': 'decreased' if mich_sim < mich_orig else 'increased',
            'change_percent': ((mich_orig - mich_sim) / mich_orig) * 100 if mich_orig > 0 else 0
        }
    
    # ==================== FARB METRIKEN ====================
    
    def lab_b_shift(self):
        """Lab b* Shift (Blau-Gelb-Achse)"""
        b_orig = np.mean(self.orig_lab[:, :, 2])
        b_sim = np.mean(self.sim_lab[:, :, 2])
        shift = b_sim - b_orig
        
        return {
            'original': b_orig,
            'simulated': b_sim,
            'shift': shift,
            'direction': 'yellow' if shift > 0 else 'blue' if shift < 0 else 'unchanged'
        }
    
    def color_gamut_a(self):
        """Color Gamut auf a*-Achse (Rot-Grün)"""
        a_orig_range = np.max(self.orig_lab[:, :, 1]) - np.min(self.orig_lab[:, :, 1])
        a_sim_range = np.max(self.sim_lab[:, :, 1]) - np.min(self.sim_lab[:, :, 1])
        
        return {
            'original': a_orig_range,
            'simulated': a_sim_range,
            'direction': 'decreased' if a_sim_range < a_orig_range else 'increased',
            'change_percent': ((a_orig_range - a_sim_range) / a_orig_range) * 100 if a_orig_range > 0 else 0
        }
    
    # ==================== NEUE FARB-METRIKEN ====================
    
    def saturation_mean(self):
        """Mittlere Sättigung im HSV-Farbraum"""
        hsv_orig = cv2.cvtColor(self.orig_rgb, cv2.COLOR_RGB2HSV)
        hsv_sim = cv2.cvtColor(self.sim_rgb, cv2.COLOR_RGB2HSV)
        
        sat_orig = np.mean(hsv_orig[:, :, 1])
        sat_sim = np.mean(hsv_sim[:, :, 1])
        
        return {
            'original': sat_orig,
            'simulated': sat_sim,
            'direction': 'decreased' if sat_sim < sat_orig else 'increased',
            'change_percent': ((sat_orig - sat_sim) / sat_orig) * 100 if sat_orig > 0 else 0
        }
    
    def blue_channel_mean(self):
        """Mittlerer Blaukanal-Wert"""
        blue_orig = np.mean(self.orig_rgb[:, :, 2])
        blue_sim = np.mean(self.sim_rgb[:, :, 2])
        
        return {
            'original': blue_orig,
            'simulated': blue_sim,
            'direction': 'decreased' if blue_sim < blue_orig else 'increased',
            'change_percent': ((blue_orig - blue_sim) / blue_orig) * 100 if blue_orig > 0 else 0
        }
    
    # ==================== VERZERRUNG METRIKEN ====================
    
    def mean_displacement(self):
        """Mean Pixel Displacement via Optical Flow"""
        flow = cv2.calcOpticalFlowFarneback(
            self.orig_gray, self.sim_gray, None,
            pyr_scale=0.5, levels=3, winsize=15,
            iterations=3, poly_n=5, poly_sigma=1.2, flags=0
        )
        
        magnitude = np.sqrt(flow[..., 0]**2 + flow[..., 1]**2)
        mean_disp = np.mean(magnitude)
        
        return {
            'displacement_pixels': mean_disp,
            'direction': 'distorted' if mean_disp > 2 else 'minimal'
        }
    
    # ==================== GESICHTSFELD METRIKEN ====================
    
    def effective_fov(self):
        """Effective Field of View Percentage"""
        h, w = self.sim_gray.shape
        center_y, center_x = h // 2, w // 2
        
        y, x = np.ogrid[:h, :w]
        dist_from_center = np.sqrt((x - center_x)**2 + (y - center_y)**2)
        max_dist = np.sqrt(center_x**2 + center_y**2)
        
        sim_brightness = self.sim_gray.astype(float)
        total_brightness = np.sum(sim_brightness)
        threshold = 0.9 * total_brightness
        
        for radius in range(0, int(max_dist), 5):
            mask = dist_from_center <= radius
            cumsum = np.sum(sim_brightness[mask])
            if cumsum >= threshold:
                effective_fov = (radius / max_dist) * 100
                break
        else:
            effective_fov = 100.0
        
        return {
            'fov_percent': effective_fov,
            'direction': 'restricted' if effective_fov < 90 else 'normal'
        }
    
    def scotoma_coverage(self):
        """Scotoma Coverage Percentage"""
        diff = np.abs(self.orig_gray.astype(float) - self.sim_gray.astype(float))
        threshold = 50
        scotoma_mask = diff > threshold
        coverage = (np.sum(scotoma_mask) / scotoma_mask.size) * 100
        
        return {
            'coverage_percent': coverage,
            'direction': 'present' if coverage > 5 else 'minimal'
        }
    
    def visible_content_area(self):
        """
        Misst den Prozentsatz sichtbarer (nicht-schwarzer) Bildfläche
        Wichtig für Minifikations-Kompensation bei Tunnelblick
        """
        # Pixel > 10 gelten als sichtbar (nicht rein schwarz)
        visible_orig = np.sum(self.orig_gray > 10)
        visible_sim = np.sum(self.sim_gray > 10)
        
        total_pixels = self.orig_gray.size
        
        coverage_orig = (visible_orig / total_pixels) * 100
        coverage_sim = (visible_sim / total_pixels) * 100
        
        return {
            'original': coverage_orig,
            'simulated': coverage_sim,
            'direction': 'decreased' if coverage_sim < coverage_orig else 'increased',
            'change_percent': ((coverage_orig - coverage_sim) / coverage_orig) * 100 if coverage_orig > 0 else 0
        }
    
    def mean_luminance(self):
        """Mittlere Helligkeit"""
        mean_orig = np.mean(self.orig_gray)
        mean_sim = np.mean(self.sim_gray)
        
        return {
            'original': mean_orig,
            'simulated': mean_sim,
            'direction': 'decreased' if mean_sim < mean_orig else 'increased',
            'change_percent': ((mean_orig - mean_sim) / mean_orig) * 100 if mean_orig > 0 else 0
        }
    
    def scotoma_boundary_gradient(self):
        """
        Misst die Kantenschärfe im Ring um das Skotom
        Wichtig für Kantenbetonung am Skotomrand
        """
        # 1. Skotom-Maske finden
        diff = cv2.absdiff(self.orig_gray, self.sim_gray)
        _, scotoma_mask = cv2.threshold(diff, 50, 255, cv2.THRESH_BINARY)
        
        # 2. Ring erstellen (10-Pixel-Band um Skotom)
        kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (11, 11))
        dilated = cv2.dilate(scotoma_mask, kernel, iterations=1)
        eroded = cv2.erode(scotoma_mask, kernel, iterations=1)
        ring_mask = dilated - eroded
        
        # 3. Gradient berechnen
        gx, gy = np.gradient(self.sim_gray.astype(float))
        gradient_magnitude = np.sqrt(gx**2 + gy**2)
        
        # 4. Gradient nur im Ring messen
        if np.sum(ring_mask) > 0:
            ring_gradient = np.mean(gradient_magnitude[ring_mask > 0])
        else:
            ring_gradient = 0.0
        
        return {
            'boundary_gradient': ring_gradient,
            'direction': 'enhanced' if ring_gradient > 5 else 'minimal'
        }
    
    # ==================== FLOATER METRIKEN ====================
    
    def floater_count(self):
        """
        Zählt diskrete Floater-Objekte via Connected Components
        """
        diff = cv2.absdiff(self.orig_gray, self.sim_gray)
        _, binary = cv2.threshold(diff, 80, 255, cv2.THRESH_BINARY)
        
        kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (5, 5))
        binary = cv2.morphologyEx(binary, cv2.MORPH_OPEN, kernel, iterations=2)
        binary = cv2.morphologyEx(binary, cv2.MORPH_CLOSE, kernel, iterations=1)
        
        num_labels, labels, stats, _ = cv2.connectedComponentsWithStats(
            binary, connectivity=8
        )
        
        h, w = self.orig_gray.shape
        image_area = h * w
        min_area = int(image_area * 0.0001)
        max_area = int(image_area * 0.015)
        
        valid_floaters = 0
        floater_sizes = []
        
        for i in range(1, num_labels):
            area = stats[i, cv2.CC_STAT_AREA]
            if min_area <= area <= max_area:
                valid_floaters += 1
                floater_sizes.append(area)
        
        return {
            'floater_count': valid_floaters,
            'mean_size_pixels': np.mean(floater_sizes) if floater_sizes else 0,
            'direction': 'present' if valid_floaters > 5 else 'minimal'
        }
    
    def floater_coverage(self):
        """Prozentuale Flächenbedeckung durch Floaters"""
        diff = cv2.absdiff(self.orig_gray, self.sim_gray)
        threshold = 60
        floater_mask = diff > threshold
        coverage = (np.sum(floater_mask) / floater_mask.size) * 100
        
        return {
            'coverage_percent': coverage,
            'direction': 'present' if coverage > 2 else 'minimal'
        }
    
    def central_gradient_magnitude(self):
        """
        Gradient Magnitude nur im sichtbaren zentralen Bereich
        Für Minifikation bei Tunnelblick
        """
        # Masken für sichtbare Bereiche (nicht schwarz)
        visible_mask_orig = self.orig_gray > 10
        visible_mask_sim = self.sim_gray > 10
        
        # Gradienten berechnen
        gx_o, gy_o = np.gradient(self.orig_gray.astype(float))
        gx_s, gy_s = np.gradient(self.sim_gray.astype(float))
        
        grad_magnitude_orig = np.sqrt(gx_o**2 + gy_o**2)
        grad_magnitude_sim = np.sqrt(gx_s**2 + gy_s**2)
        
        # Mittelwert nur im sichtbaren Bereich
        if np.sum(visible_mask_orig) > 0:
            grad_orig = np.mean(grad_magnitude_orig[visible_mask_orig])
        else:
            grad_orig = 0.0
        
        if np.sum(visible_mask_sim) > 0:
            grad_sim = np.mean(grad_magnitude_sim[visible_mask_sim])
        else:
            grad_sim = 0.0
        
        return {
            'original': grad_orig,
            'simulated': grad_sim,
            'direction': 'increased' if grad_sim > grad_orig else 'decreased',
            'change_percent': ((grad_sim - grad_orig) / grad_orig) * 100 if grad_orig > 0 else 0
        }


# ==================== KOMPENSATIONS-EVALUATOR ====================

class CompensationEvaluator:
    """
    Evaluiert die Wirksamkeit von Kompensationsmethoden
    durch Vergleich: Baseline → Simulation → Kompensation
    """
    
    def __init__(self, baseline_path, simulated_path, compensated_path):
        """
        Args:
            baseline_path: Original ohne Filter
            simulated_path: Mit Sehproblem-Simulation
            compensated_path: Mit Sehproblem + Kompensation
        """
        # Drei Evaluatoren erstellen
        self.baseline_vs_sim = VisionImpairmentEvaluator(baseline_path, simulated_path)
        self.baseline_vs_comp = VisionImpairmentEvaluator(baseline_path, compensated_path)
        
        # Direkter Zugriff auf Bilder
        self.baseline = self.baseline_vs_sim.orig_rgb
        self.simulated = self.baseline_vs_sim.sim_rgb
        self.compensated = self.baseline_vs_comp.sim_rgb
    
    def evaluate_compensation(self, metric_name):
        """
        Evaluiert eine Kompensation für eine spezifische Metrik
        """
        baseline_sim_result = getattr(self.baseline_vs_sim, metric_name)()
        baseline_comp_result = getattr(self.baseline_vs_comp, metric_name)()
        
        baseline_value = self._extract_value(baseline_sim_result, 'original')
        simulated_value = self._extract_value(baseline_sim_result, 'simulated')
        compensated_value = self._extract_value(baseline_comp_result, 'simulated')
        
        degradation = baseline_value - simulated_value
        improvement = compensated_value - simulated_value
        
        # SPEZIALFALL: 2-Stufen-Vergleich (z.B. Skotomrand, Minifikation)
        if abs(degradation) < 0.001:
            # Baseline ≈ Simuliert → Messe direkte Verstärkung
            if baseline_value > 0:
                restoration_percent = (improvement / baseline_value) * 100
            else:
                restoration_percent = 0.0
            degradation_percent = 0.0
        else:
            # Normal: 3-Stufen-Vergleich
            restoration_percent = (improvement / degradation) * 100
            degradation_percent = (degradation / baseline_value) * 100 if baseline_value > 0 else 0
        
        return {
            'baseline': baseline_value,
            'simulated': simulated_value,
            'compensated': compensated_value,
            'degradation_percent': degradation_percent,
            'improvement': improvement,
            'restoration_percent': restoration_percent,
            'effective': restoration_percent > 30
        }
    
    def _extract_value(self, result, key):
        """
        Extrahiert Wert aus Metrik-Result (verschiedene Formate)
        """
        if key in result:
            return result[key]
        
        # Fallbacks für spezielle Metriken
        if 'fov_percent' in result:
            return result['fov_percent']
        if 'coverage_percent' in result:
            return result['coverage_percent']
        if 'boundary_gradient' in result:
            return result['boundary_gradient']
        if 'displacement_pixels' in result:
            return result['displacement_pixels']
        if 'shift' in result:
            return result['shift']
        
        return 0.0
    
    def evaluate_multiple_metrics(self, metrics_list):
        """
        Evaluiert mehrere Metriken für eine Kompensation
        
        Args:
            metrics_list: Liste von Metrik-Namen
        
        Returns:
            dict mit Ergebnissen pro Metrik
        """
        results = {}
        
        for metric_name in metrics_list:
            try:
                results[metric_name] = self.evaluate_compensation(metric_name)
            except Exception as e:
                print(f"⚠️  Fehler bei {metric_name}: {e}")
                results[metric_name] = None
        
        return results
    
    def print_evaluation(self, compensation_name, metrics_list):
        """
        Gibt Evaluationsergebnisse formatiert aus
        
        Args:
            compensation_name: Name der Kompensation (z.B. "Kantenbetonung")
            metrics_list: Liste von Metrik-Namen
        """
        print("\n" + "=" * 80)
        print(f"KOMPENSATIONS-EVALUATION: {compensation_name.upper()}")
        print("=" * 80)
        
        results = self.evaluate_multiple_metrics(metrics_list)
        
        for metric_name, result in results.items():
            if result is None:
                continue
            
            print(f"\n{metric_name.upper().replace('_', ' ')}:")
            print(f"   Baseline:          {result['baseline']:>10.2f}")
            print(f"   Simuliert:         {result['simulated']:>10.2f}  "
                  f"({result['degradation_percent']:>+6.1f}%)")
            print(f"   Kompensiert:       {result['compensated']:>10.2f}")
            print(f"   Wiederherstellung: {result['restoration_percent']:>10.1f}%")
            
            if result['effective']:
                print(f"   Status:            ✓ WIRKSAM")
            else:
                print(f"   Status:            ✗ UNZUREICHEND")
        
        # Gesamtbewertung
        print("\n" + "=" * 80)
        effective_count = sum(1 for r in results.values() if r and r['effective'])
        total_count = len([r for r in results.values() if r is not None])
        
        print(f"GESAMT: {effective_count}/{total_count} Metriken zeigen wirksame Kompensation")
        
        if effective_count == total_count:
            print("✓ Kompensation VOLLSTÄNDIG wirksam")
        elif effective_count > 0:
            print("⚠ Kompensation TEILWEISE wirksam")
        else:
            print("✗ Kompensation UNWIRKSAM")
        
        print("=" * 80 + "\n")
        
        return results


# ==================== SEHPROBLEM-EVALUATOR (ORIGINAL) ====================

def evaluate_vision_problem(problem_name, original_path, simulated_path, metrics_to_use):
    """Evaluiert ein spezifisches Sehproblem"""
    print("=" * 80)
    print(f"{problem_name.upper()} - EVALUATION")
    print("=" * 80)
    
    evaluator = VisionImpairmentEvaluator(original_path, simulated_path)
    
    if not evaluator.validate_pair():
        print("\n⚠️ Bildpaar nicht geeignet. Neue Screenshots erstellen!\n")
        return None
    
    results = {'problem': problem_name, 'metrics': {}}
    
    print("\n📊 METRIKEN:\n")
    
    for metric_name in metrics_to_use:
        method = getattr(evaluator, metric_name)
        result = method()
        results['metrics'][metric_name] = result
        
        print(f"   {metric_name.upper().replace('_', ' ')}:")
        
        if 'original' in result and 'simulated' in result:
            print(f"      Original:    {result['original']:>10.2f}")
            print(f"      Simuliert:   {result['simulated']:>10.2f}")
            if 'change_percent' in result:
                print(f"      Änderung:    {result['change_percent']:>10.1f}%")
            print(f"      Richtung:    {result['direction']}")
        elif 'shift' in result:
            print(f"      Original:    {result['original']:>10.2f}")
            print(f"      Simuliert:   {result['simulated']:>10.2f}")
            print(f"      Shift:       {result['shift']:>+10.2f}")
            print(f"      Richtung:    {result['direction']}")
        else:
            for key, value in result.items():
                print(f"      {key}: {value}")
        
        print()
    
    print("=" * 80)
    print("BEWERTUNG:")
    print("=" * 80)
    
    all_correct = True
    for metric_name, result in results['metrics'].items():
        if 'direction' in result:
            expected = get_expected_direction(problem_name, metric_name)
            actual = result['direction']
            
            if expected and actual != expected:
                all_correct = False
                print(f"⚠️  {metric_name}: Erwartet '{expected}', gefunden '{actual}'")
    
    if all_correct:
        print(f"✅ {problem_name}-Simulation validiert")
        print(f"   Alle Metriken zeigen erwartete Richtung")
    else:
        print(f"⚠️  {problem_name}-Simulation teilweise validiert")
        print(f"   Einige Metriken zeigen unerwartete Richtung")
    
    print("\n")
    
    return results


def get_expected_direction(problem_name, metric_name):
    """Gibt erwartete Richtung für Metrik zurück"""
    expectations = {
        'Myopie': {
            'laplacian_variance': 'decreased',
            'gradient_magnitude': 'decreased'
        },
        'Hyperopie': {
            'laplacian_variance': 'decreased',
            'gradient_magnitude': 'decreased'
        },
        'Astigmatismus': {
            'laplacian_variance': 'decreased',
            'mean_displacement': 'distorted'
        },
        'Katarakt': {
            'laplacian_variance': 'decreased',
            'rms_contrast': 'decreased',
            'lab_b_shift': 'yellow'
        },
        'Glaukom': {
            'scotoma_coverage': 'present',
            'rms_contrast': 'increased'
        },
        'Protanopie': {
            'color_gamut_a': 'decreased'
        },
        'AMD': {
            'scotoma_coverage': 'present',
            'mean_displacement': 'distorted'
        },
        'Diabetische_Retinopathie': {
            'scotoma_coverage': 'present',
            'rms_contrast': 'increased'
        },
        'Retinitis_Pigmentosa': {
            'scotoma_coverage': 'present',
            'rms_contrast': 'increased'
        },
        'Glaskoerpertruebungen': {
            'floater_count': 'present',
            'floater_coverage': 'present'
        }
    }
    
    return expectations.get(problem_name, {}).get(metric_name)


# ==================== BEISPIEL-VERWENDUNG ====================

if __name__ == "__main__":
    # ======== BEISPIEL 1: SEHPROBLEM-EVALUATION ========
    print("\n" + "="*80)
    print("BEISPIEL 1: SEHPROBLEM-EVALUATION")
    print("="*80)
    
    comp_evaluator = CompensationEvaluator(
        baseline_path="",
        simulated_path="",
        compensated_path=""
    )

    comp_evaluator.print_evaluation(
        compensation_name="Kantenbetonung",
        metrics_list=['scotoma_boundary_gradient']
    )

    evaluate_vision_problem(
    problem_name="Myopie",
    original_path="",
    simulated_path="",
    metrics_to_use=['laplacian_variance', 'gradient_magnitude']
)
