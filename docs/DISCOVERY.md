# DISCOVERY & RANKING ARCHITECTURE

## 1. Intent Separation
Bookline's discovery engine explicitly separates search intent:
- **Service Intent**: "haircut", "facial", "pilates", "teeth whitening"
- **Category Intent**: "Beauty", "Wellness", "Healthcare", "Fitness"
- **Geographic Intent**: "near me", "Ahmedabad", "Bandra Mumbai", "Indiranagar"
- **Provider Intent**: direct name lookup ("Aura Wellness", "Glow Lounge")

## 2. Multi-Signal Ranking Algorithm
The ranking service calculates a normalized score using weighted signals:
- **Text & Service Relevance (35%)**: Direct matches on provider name, description, and catalog service offerings.
- **Geographic Proximity (25%)**: Distance calculated via PostGIS spatial engine or Haversine formula against client lat/long coordinates.
- **Customer Rating (20%)**: Historical client rating on a 5.0 scale.
- **Review Volume (10%)**: Logarithmic scaling of verified customer reviews.
- **Platform Verification Bonus (20%)**: Bonus score granted to verified businesses.

## 3. Supported Sorting Strategies
- `Recommended`: Evaluates composite multi-signal ranking score.
- `Nearest`: Ascending distance order from requested coordinates.
- `Top rated`: Average customer review rating.
- `Most reviewed`: Total review count.
- `Lowest price`: Minimum service starting price.
- `Highest price`: Premium service starting price.
