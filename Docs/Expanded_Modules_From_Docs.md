Summary of modules added from Docs/Upholstery_Calculation_Suite.docx and Docs/Woodworking_Calculation_Suite.docx

Upholstery modules (new)
- Fabric Yardage Calculator (pattern repeat-aware, orientation/warp, fabric width fit, waste calc)
- Foam Volume & Density Calculator (volume, density estimate, weight)
- Cushion Cutting/Layout generator (top/bottom/boxing pieces, seam allowances, inner lining pieces)
- Inner Cover & Lining generator (zippers, seam allowances)
- Tufting Layout generator (button grid placement, spacing)
- Pattern Matching assistant (repeat alignment, adjusted lengths)
- Cost Estimator (material + labor + foam)
- Cutting Optimization (best-fit nesting, fabric width aware)
- DXF/PDF export of pattern pieces at 1:1 (mm)

Woodwork modules (new)
- Segmented Ring Calculator (angles, edge lengths, kerf compensation)
- Miter & Compound Miter Calculator (miter/bevel angles)
- Arc Length & Arc Calculator (arc length from radius/angle)
- Right Triangle / Diagonal helper (Pythagorean calculations)
- Staircase Calculator (risers/treads, rule checks)
- Board Foot / Volume Estimator (material estimation)
- Joinery Assist (mortise & tenon, dovetail presets)
- Polygon Layout & Panelization tools
- DXF export for CNC (layers: cut/engrave/annotations)

Recommended next steps
1. Add service skeletons in NativeElements.Services: FabricCalculatorService, FoamCalculatorService, CuttingLayoutService, PatternMatchingService, CostEstimatorService, CuttingOptimizerService, GeometryService, JoineryService, BoardFootCalculator, StaircaseService.
2. Add MVVM ViewModels + Views for each new calculator (start with Fabric Yardage and Segmented Ring enhancements).
3. Implement unit tests in NativeElements.Core for critical math (fabric repeat math, foam volume, segment geometry).
4. Add todos for implementation and integration testing.

This file was generated from the project Docs and incorporated into the plan.
