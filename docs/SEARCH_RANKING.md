# Bookline Search Relevance & Ranking Engine

## 1. Overview & Architectural Philosophy

Bookline implements a transparent, defensible, and deterministic multi-signal ranking strategy for marketplace discovery. The system explicitly rejects equating "best" with raw review scores—a newly registered provider with a single five-star review must never automatically outrank an established provider with hundreds of strong reviews.

Rather than claiming opaque "AI recommendation models", the ranking system is built upon **deterministic, versioned weighted scoring** (`IProviderRankingService` v1.0.0). It evaluates only signals the platform legitimately possesses, handles missing data intentionally without fabricating values, and enforces strict relevance gating.

---

## 2. Architecture & Modular Decomposition

The ranking engine decouples evaluation into five independent, unit-testable components:

```
                      +-----------------------------+
                      |   IProviderRankingService   |
                      |          (v1.0.0)           |
                      +--------------+--------------+
                                     |
       +-----------------+-----------+-----------+-----------------+
       |                 |                       |                 |
+------v------+   +------v------+         +------v------+   +------v------+
|  Relevance  |   |  Geographic |         |   Quality   |   |    Trust    |
|  Component  |   |  Component  |         |  Component  |   |  Component  |
+-------------+   +-------------+         +-------------+   +-------------+
                                     |
                              +------v------+
                              | Availability|
                              |  Component  |
                              +-------------+
```

Each component evaluates a specialized facet of the candidate provider location:

| Component | Interface | Description |
| :--- | :--- | :--- |
| **Relevance** | `IRelevanceScoreComponent` | Query text match, exact service match, category match, and neighborhood match. |
| **Geographic** | `IGeographicScoreComponent` | Haversine distance rational decay curve and neighborhood boundary alignment. |
| **Quality** | `IQualityScoreComponent` | Bayesian smoothed review rating and logarithmic review volume confidence. |
| **Trust** | `ITrustScoreComponent` | Verified provider badge, profile completeness, and operational reliability. |
| **Availability** | `IAvailabilityScoreComponent` | Real slot availability within booking window (evaluated only when availability engine provides data). |

---

## 3. Signal Formulation & Mathematical Models

### 3.1 Quality Scoring: Bayesian Smoothed Rating

To prevent low-volume sample bias (e.g. 1 review of 5.0★ beating 120 reviews of 4.9★), ratings are smoothed using an $m$-estimate Bayesian formulation:

$$\text{SmoothedRating}(R, v) = \frac{R \cdot v + C \cdot m}{v + m}$$

Where:
- $R$ = Provider's actual arithmetic mean rating.
- $v$ = Provider's verified review count.
- $C = 4.0$ = Marketplace prior baseline rating.
- $m = 5$ = Damping weight (equivalent to 5 virtual reviews at the marketplace prior).

#### Mathematical Demonstration
1. **Established Provider** ($R = 4.9$, $v = 120$):
   $$\text{SmoothedRating} = \frac{4.9 \times 120 + 4.0 \times 5}{120 + 5} = \frac{588 + 20}{125} = \frac{608}{125} = \mathbf{4.864}$$
2. **Single-Review Provider** ($R = 5.0$, $v = 1$):
   $$\text{SmoothedRating} = \frac{5.0 \times 1 + 4.0 \times 5}{1 + 5} = \frac{5.0 + 20}{6} = \frac{25}{6} = \mathbf{4.167}$$

**Outcome**: The established provider ($4.864$) decisively outranks the single-review provider ($4.167$).

In addition to smoothed rating, quality incorporates a review volume confidence factor:
$$\text{VolumeConfidence}(v) = \min\left(1.0, \frac{\log_{10}(v + 1)}{2.0}\right)$$
This saturates at approximately 100 reviews.

---

### 3.2 Geographic Proximity: Rational Decay

When reference coordinates (transient GPS or selected place center) are present, distance decays smoothly:

$$\text{ProximityScore}(d) = \frac{1.0}{1.0 + \frac{d}{D_0}}$$

Where:
- $d$ = Geodesic distance in kilometers ($d = \text{DistanceMeters} / 1000.0$).
- $D_0 = 8.0\text{ km}$ = Characteristic half-decay radius.

**Behavior**:
- $0\text{ km} \rightarrow 1.00$
- $2\text{ km} \rightarrow 0.80$
- $8\text{ km} \rightarrow 0.50$
- $24\text{ km} \rightarrow 0.25$

#### Missing Distance Behavior
If the customer has not provided reference coordinates (e.g., GPS permission denied or macro city search without coordinates), **distance is never fabricated**. The geographic component returns a neutral baseline ($0.50$), and `DistanceMeters` in the response remains `null`.

---

### 3.3 Relevance Gating & Service Match Rule

In the default **Recommended** sort:
- **Search relevance acts as the primary gatekeeper**.
- A provider that **does not offer the requested service** must never outrank a relevant provider simply because it has a slightly higher overall rating.

When a query requests a specific service (e.g., "Reformer Pilates"):
- Providers offering the exact service receive full relevance ($1.0$).
- Providers lacking the service receive a severe relevance penalty ($0.05$).
- When relevance falls below $0.20$, an overall **relevance gate multiplier ($0.40$)** is applied to the final score, guaranteeing that irrelevant venues cannot jump above relevant venues.

---

### 3.4 Trust & Profile Completeness

The trust component combines:
1. **Verification Status (40%)**: Verified badge assigned by platform administrators.
2. **Profile Completeness (40%)**:
   - High-resolution logo: $+10\%$
   - Cover hero image: $+10\%$
   - Detailed public description ($>20$ chars): $+10\%$
   - Complete contact & address info: $+10\%$
3. **Operational Status (20%)**: Published and active storefront.

---

### 3.5 Dynamic Weighting & Availability Absence

When availability data is queried and present:
- **Relevance**: $0.35$
- **Geographic Proximity**: $0.20$
- **Quality**: $0.20$
- **Trust**: $0.15$
- **Availability**: $0.10$

When availability data is **not evaluated** or unavailable:
The availability signal is omitted and documented in the explanation. The weights renormalize cleanly to sum to $1.00$:
- **Relevance**: $0.38$
- **Geographic Proximity**: $0.22$
- **Quality**: $0.23$
- **Trust**: $0.17$
- **Availability**: $0.00$

---

## 4. Supported Sort Modes & Tie-Breakers

Every supported sort defines deterministic primary, secondary, and tertiary sorting so that pagination never causes items to unpredictably shift between pages:

| Sort Mode | Primary Sort | Secondary Sort | Final Tie-Breaker | Fallback When Data Missing |
| :--- | :--- | :--- | :--- | :--- |
| **Recommended** | `RankingScore` Descending | Bayesian Rating Descending | `LocationId` Ascending | N/A |
| **Nearest** | `DistanceMeters` Ascending | Bayesian Rating Descending | `LocationId` Ascending | Falls back to Recommended sort order |
| **Top Rated** | Bayesian Rating Descending | `ReviewCount` Descending | `LocationId` Ascending | Unreviewed venues placed at bottom |
| **Most Reviewed**| `ReviewCount` Descending | Bayesian Rating Descending | `LocationId` Ascending | N/A |
| **Earliest Available**| `NextAvailableAt` Ascending | Bayesian Rating Descending | `LocationId` Ascending | Null availability placed at end |
| **Lowest Price**| `StartingPrice` Ascending | Bayesian Rating Descending | `LocationId` Ascending | Unpriced venues placed at end |
| **Highest Price**| `StartingPrice` Descending| Bayesian Rating Descending | `LocationId` Ascending | Unpriced venues placed at end |

---

## 5. Customer Location Privacy

1. **Transient GPS by Default**: Customer GPS coordinates provided via browser geolocation remain transient in memory. They are never automatically saved to customer database profiles without explicit opt-in.
2. **Sanitized Shareable URLs**: Exact coordinates are never placed in public shareable discovery URLs. Shareable links utilize normalized slugs, e.g.:
   - `?city=mumbai&area=bandra`
   - `?place=area-bodakdev-ahmedabad`
   - `?postcode=380054`
   - `?city=ahmedabad&nearme=true`

---

## 6. Promoted & Sponsored Content Invariants

If paid placements or sponsored listings are introduced:
- They must remain explicitly flagged with `IsPromoted: true`.
- Organic ranking scores must never be artificially inflated.
- Promoted slots are reserved in designated carousel or banner positions, ensuring organic discovery remains authentic and trust-based.
