# Landing Page Documentation

Complete documentation for the Persian/RTL customer-facing landing page.

---

## Overview

The landing page is a modern, responsive, Persian/RTL interface designed to attract and convert customers. It follows best practices from platforms like AsanRezerve and Booking.com with a focus on beauty and wellness services.

**Location:** `src/views/HomeView.vue` (main container)
**Components:** `src/components/landing/`

---

## Components

### 1. HeroSection.vue

**Purpose:** Main hero section with search functionality and background video

**Features:**
- 🎥 **Background Video:** Beauty salon reflection video from Coverr
  - Source: `https://cdn.coverr.co/videos/coverr-beauty-salon-reflection-8122/1080p.mp4`
  - Fallback poster: Unsplash beauty salon image
  - Subtle 30s zoom animation for dynamic feel
  - Hidden on mobile (<768px) to save bandwidth
  - Opacity: 30% with brightness/contrast filters

- 🔍 **Search Interface:**
  - Two search modes: "جستجوی خدمات" (Service Search) and "نزدیک من" (Near Me)
  - Service input with autocomplete
  - Location input (city, state)
  - Quick search tags for popular categories

- 📊 **Stats Display:**
  - ۱۰,۰۰۰+ service providers
  - ۵۰,۰۰۰+ happy customers
  - ۴.۸/۵ average rating (Persian numbers)

**Z-Index Layering:**
```
Video (z-index: 0)
  ↓
Gradient Overlay (z-index: 1, 85% opacity)
  ↓
Pattern Overlay (z-index: 2)
  ↓
Content (z-index: 3)
```

**Styling:**
- RTL layout with `dir="rtl"`
- Vazir font (applied globally)
- Purple-to-pink gradient overlay
- Glassmorphism effects on search card
- Responsive padding and font sizes

**Key Functions:**
- `handleSearch()` - Navigate to provider search with filters
- `quickSearch(category)` - Jump to specific category

---

### 2. CategoryGrid.vue

**Purpose:** Display 8 service categories with beautiful gradient cards

**Categories:**
1. **آرایشگاه مو** (Hair Salon) - ۲,۵۰۰+ providers - Purple gradient 💇
2. **ماساژ و اسپا** (Massage & Spa) - ۱,۸۰۰+ providers - Pink gradient 💆
3. **پاکسازی پوست** (Facial) - ۱,۲۰۰+ providers - Blue gradient ✨
4. **مانیکور و پدیکور** (Nails) - ۱,۵۰۰+ providers - Orange-yellow gradient 💅
5. **آرایش** (Makeup) - ۹۰۰+ providers - Peach gradient 💄
6. **اپیلاسیون** (Waxing) - ۸۰۰+ providers - Aqua-pink gradient 🌿
7. **آرایشگاه مردانه** (Barbershop) - ۱,۱۰۰+ providers - Gold-blue gradient 💈
8. **خالکوبی و پیرسینگ** (Tattoo) - ۶۰۰+ providers - Pink-red gradient 🎨

**Interactions:**
- Hover: Card lifts up (-8px) with shadow
- Icon scales and rotates (1.1x, 5deg)
- Arrow appears from left side (RTL)
- Staggered animation on load (0.1s-0.8s delays)

**Grid Layout:**
- Desktop: `repeat(auto-fit, minmax(260px, 1fr))`
- Mobile: `repeat(auto-fit, minmax(140px, 1fr))`

**Navigation:**
```typescript
navigateToCategory(slug: string) {
  router.push({
    path: '/providers/search',
    query: { serviceCategory: slug }
  })
}
```

---

### 3. FeaturedProviders.vue

**Purpose:** Showcase top-rated providers with real data from API

**Features:**
- ⭐ **Top-Rated Badge:** Gold gradient badge for providers with 4.5+ rating
- 🖼️ **Mock Images:** 6 high-quality Unsplash images rotating by index:
  ```typescript
  const mockImages = [
    'photo-1560066984-138dadb4c035', // Hair salon
    'photo-1540555700478-4be289fbecef', // Spa/Massage
    'photo-1487412947147-5cebf100ffc2', // Facial
    'photo-1604902396830-aca29bb5b2e2', // Nails
    'photo-1522337360788-8b13dee7a37e', // Makeup
    'photo-1519415510236-718bdfcd89c8', // Barbershop
  ]
  ```
- 📊 **Persian Numbers:** Automatic conversion for ratings and review counts
- 🏷️ **Feature Badges:** "رزرو آنلاین" (Online Booking), "خدمات سیار" (Mobile Service)

**Data Flow:**
```typescript
onMounted(async () => {
  await providerStore.searchProviders({
    pageNumber: 1,
    pageSize: 6,
    sortBy: 'rating',
    sortDescending: true,
  })
  featuredProviders.value = providerStore.providers.slice(0, 6)
})
```

**Card Structure:**
- Provider image with hover zoom (1.1x scale)
- Top-rated badge (left side for RTL)
- Business name and rating
- Description (truncated to 80 chars)
- Location and price indicator
- Feature badges
- "رزرو کنید" (Book Now) button

**Actions:**
- `viewProvider(id)` - Navigate to provider detail page
- `bookProvider(id)` - Navigate to booking page
- `viewAll()` - Navigate to search page

---

### 4. HowItWorks.vue

**Purpose:** Explain 3-step booking process

**Steps:**

**۱. جستجو و کشف** (Search & Discover)
- Icon: 🔍
- Color: Purple gradient
- Features:
  - فیلتر بر اساس نوع خدمات (Filter by service type)
  - بررسی امتیازات و نظرات (Check ratings & reviews)
  - مشاهده دسترسی آنلاین (View real-time availability)

**۲. رزرو فوری** (Book Instantly)
- Icon: 📅
- Color: Pink gradient
- Features:
  - تایید فوری (Instant confirmation)
  - پرداخت امن آنلاین (Secure online payment)
  - همگام‌سازی با تقویم (Calendar sync)

**۳. لذت ببرید** (Enjoy & Relax)
- Icon: ✨
- Color: Blue gradient
- Features:
  - تغییر زمان آسان (Easy rescheduling)
  - رسید دیجیتال (Digital receipts)
  - جوایز وفاداری (Loyalty rewards)

**Design:**
- Step cards with glassmorphism background
- Gradient step numbers at top (-20px offset)
- Staggered animation (0.2s delays)
- Hover: Card lifts (-8px) with shadow
- CTA section with gradient background and pattern

**CTA:**
- Title: "آماده شروع هستید؟" (Ready to get started?)
- Button: "ارائه‌دهنده خود را پیدا کنید" (Find Your Provider)
- Navigates to `/providers/search`

---

### 5. Testimonials.vue

**Purpose:** Display customer reviews and satisfaction statistics

**Testimonials (6 Persian Reviews):**

1. **سارا احمدی** - کوتاهی و آرایش مو
2. **محمد رضایی** - ماساژ درمانی
3. **مریم کریمی** - پاکسازی پوست
4. **علی محمدی** - آرایشگاه مردانه
5. **فاطمه حسینی** - مانیکور و پدیکور
6. **رضا نوری** - پکیج اسپا

Each includes:
- 5-star rating display
- Service type
- Persian testimonial text
- "مشتری تایید شده" (Verified Customer) badge
- Avatar with initials

**Stats Row:**
- **۵۰,۰۰۰+** مشتری راضی (Happy Customers)
- **۴.۸/۵** میانگین امتیاز (Average Rating)
- **۱۰۰,۰۰۰+** رزرو تکمیل شده (Bookings Completed)
- **۹۸٪** نرخ رضایت (Satisfaction Rate)

**Layout:**
- 2-column grid on desktop
- 1-column on mobile
- Staggered animations (0.15s delays)
- Glassmorphism cards with gradient borders

---

### 6. CTASection.vue

**Purpose:** Final call-to-action for user registration

**Content:**
- **Title:** "آماده تغییر ظاهر خود هستید؟"
  (Ready to Transform Your Look?)
- **Subtitle:** "به هزاران مشتری راضی بپیوندید..."
  (Join thousands of happy customers...)

**Buttons:**
- **Primary:** "ارائه‌دهندگان نزدیک خود را پیدا کنید"
  (Find Providers Near You) → `/providers/search`
- **Secondary:** "بیشتر بدانید"
  (Learn More) → About page

**Design:**
- Purple-to-pink gradient background
- Subtle pattern overlay
- White buttons with hover effects
- RTL arrow icons
- Responsive padding

---

## Typography

### Vazir Font v16.1.0

**Installation:**
Font files located in `/public/vazir-font-v16.1.0/`

**Font Weights:**
- Thin (100): `Vazir-Thin.{eot,ttf,woff,woff2}`
- Light (300): `Vazir-Light.{eot,ttf,woff,woff2}`
- Regular (400): `Vazir.{eot,ttf,woff,woff2}`
- Medium (500): `Vazir-Medium.{eot,ttf,woff,woff2}`
- Bold (700): `Vazir-Bold.{eot,ttf,woff,woff2}`

**Global Application:**
```css
/* src/assets/fonts.css */
html[lang="fa"],
html[lang="fa"] body,
html[lang="fa"] * {
  font-family: 'Vazir', 'B Nazanin', Tahoma, 'Iranian Sans', sans-serif !important;
}
```

**Features:**
- `font-display: swap` for faster initial render
- Multiple format support (eot, ttf, woff, woff2)
- Fallback to B Nazanin font

---

## Persian Number Conversion

All numeric values are automatically converted to Persian digits:

```typescript
const convertToPersianNumber = (num: number): string => {
  const persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']
  return num.toString()
    .split('')
    .map(digit => persianDigits[parseInt(digit)] || digit)
    .join('')
}
```

**Examples:**
- `1,000` → `۱,۰۰۰`
- `4.8` → `۴.۸`
- `98%` → `۹۸٪`

Used in:
- FeaturedProviders: Ratings and review counts
- HeroSection: Stats display
- Testimonials: Statistics

---

## RTL (Right-to-Left) Support

### Key Principles

1. **Direction Attribute:**
   ```vue
   <section dir="rtl">
   ```

2. **Icon Positioning:**
   - Change `left` to `right` and vice versa
   - Reverse arrow directions in SVG paths

3. **Text Alignment:**
   ```css
   text-align: right; /* Default for RTL */
   ```

4. **Padding/Margin:**
   - Use logical properties or swap left/right
   - Example: `padding: 1rem 3rem 1rem 1rem` (icon on right)

5. **Transform Direction:**
   ```css
   /* LTR */
   transform: translateX(4px);

   /* RTL */
   transform: translateX(-4px);
   ```

### Arrow Icons (RTL Examples)

**Left Arrow (for RTL "forward"):**
```html
<path d="M15 19l-7-7 7-7" />
```

**Right Arrow (for RTL "back"):**
```html
<path d="M9 5l7 7-7 7" />
```

---

## Mock Images

### Hero Section Video

**Source:** Coverr (Free stock video)
- URL: `https://cdn.coverr.co/videos/coverr-beauty-salon-reflection-8122/1080p.mp4`
- Type: Beauty salon interior with soft lighting
- Duration: ~10 seconds (loops seamlessly)
- Poster: `https://images.unsplash.com/photo-1560066984-138dadb4c035?w=1920&q=80`

**Optimization:**
- Hidden on mobile devices
- 30% opacity for readability
- Gradient overlay (85% opacity)
- Subtle zoom animation (1x to 1.1x over 30s)

### Provider Images

**6 Unsplash Images** (optimized at w=600, q=80):

1. **Hair Salon:**
   `https://images.unsplash.com/photo-1560066984-138dadb4c035?w=600&q=80`

2. **Spa/Massage:**
   `https://images.unsplash.com/photo-1540555700478-4be289fbecef?w=600&q=80`

3. **Facial/Skincare:**
   `https://images.unsplash.com/photo-1487412947147-5cebf100ffc2?w=600&q=80`

4. **Nails:**
   `https://images.unsplash.com/photo-1604902396830-aca29bb5b2e2?w=600&q=80`

5. **Makeup:**
   `https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=600&q=80`

6. **Barbershop:**
   `https://images.unsplash.com/photo-1519415510236-718bdfcd89c8?w=600&q=80`

**Usage:**
Images rotate based on provider index: `mockImages[index % mockImages.length]`

---

## Responsive Design

### Breakpoints

**Mobile:** `max-width: 768px`
- Video hidden
- Grid becomes single column
- Reduced padding and font sizes
- Full-width buttons

**Tablet:** `max-width: 1024px`
- 2-column grids
- Adjusted spacing

**Desktop:** `min-width: 1025px`
- 3-column grids
- Full animations
- Video visible

### Grid Examples

**CategoryGrid:**
```css
/* Desktop */
grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));

/* Mobile */
grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
```

**FeaturedProviders:**
```css
/* Desktop */
grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));

/* Mobile */
grid-template-columns: 1fr;
```

---

## Animations

### CSS Animations

**fadeInUp** (used in all components):
```css
@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(30px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
```

**fadeInScale** (FeaturedProviders):
```css
@keyframes fadeInScale {
  from {
    opacity: 0;
    transform: scale(0.95);
  }
  to {
    opacity: 1;
    transform: scale(1);
  }
}
```

**slowZoom** (Hero video):
```css
@keyframes slowZoom {
  0% { transform: translate(-50%, -50%) scale(1); }
  100% { transform: translate(-50%, -50%) scale(1.1); }
}
```

### Staggered Delays

**CategoryGrid:**
```css
.category-card:nth-child(1) { animation-delay: 0.1s; }
.category-card:nth-child(2) { animation-delay: 0.2s; }
/* ... up to 0.8s for 8th card */
```

**FeaturedProviders:**
```typescript
:style="{ animationDelay: `${index * 0.1}s` }"
```

---

## Color Palette

### Gradients

**Purple (Primary):**
```css
linear-gradient(135deg, #667eea 0%, #764ba2 100%)
```

**Pink:**
```css
linear-gradient(135deg, #f093fb 0%, #f5576c 100%)
```

**Blue:**
```css
linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)
```

**Orange-Yellow:**
```css
linear-gradient(135deg, #fa709a 0%, #fee140 100%)
```

**Gold:**
```css
linear-gradient(90deg, #ffd700 0%, #ffed4e 100%)
```

### Text Colors

- **Headings:** `#1e293b` (Slate 800)
- **Body:** `#64748b` (Slate 500)
- **Muted:** `#94a3b8` (Slate 400)
- **White:** `#ffffff`

---

## Performance Optimizations

### Video Optimization
- ✅ Hidden on mobile devices
- ✅ Compressed at 1080p quality
- ✅ Poster image for instant display
- ✅ `playsinline` attribute for iOS

### Image Optimization
- ✅ Unsplash optimization params (`w=600&q=80`)
- ✅ Lazy loading (browser default)
- ✅ Error fallback handling

### Font Optimization
- ✅ `font-display: swap` for faster initial render
- ✅ WOFF2 format priority (best compression)
- ✅ Fallback font chain

### Animation Optimization
- ✅ GPU-accelerated transforms
- ✅ `will-change` for complex animations
- ✅ Reduced motion media query support

---

## State Management

### Provider Store Integration

**FeaturedProviders.vue:**
```typescript
import { useProviderStore } from '@/modules/provider/stores/provider.store'

const providerStore = useProviderStore()

onMounted(async () => {
  await providerStore.searchProviders({
    pageNumber: 1,
    pageSize: 6,
    sortBy: 'rating',
    sortDescending: true,
  })
  featuredProviders.value = providerStore.providers
})
```

**Data Flow:**
1. Component mounts
2. Fetch top 6 rated providers from API
3. Store in Pinia store
4. Display with Persian translations
5. Show mock images if no logoUrl

---

## Navigation Flow

### User Journey

```
Landing Page (HomeView)
  ↓
Hero Search / Category Click
  ↓
Provider Search (/providers/search)
  ↓
Provider Detail (/providers/:id)
  ↓
Booking (/book/:id)
```

### Route Parameters

**Search Query:**
```typescript
{
  path: '/providers/search',
  query: {
    serviceCategory: 'haircut',
    city: 'تهران',
    pageNumber: 1,
    pageSize: 12
  }
}
```

---

## Testing Recommendations

### Visual Testing
- ✅ Check RTL alignment on all components
- ✅ Verify Persian numbers display correctly
- ✅ Test responsive breakpoints (320px, 768px, 1024px, 1440px)
- ✅ Validate video plays on desktop, hidden on mobile
- ✅ Ensure images load with fallbacks

### Functional Testing
- ✅ Search navigation with filters
- ✅ Category navigation
- ✅ Provider card clicks
- ✅ CTA button navigation
- ✅ Quick search tags

### Performance Testing
- ✅ Lighthouse score >90
- ✅ First Contentful Paint <1.5s
- ✅ Video doesn't block rendering
- ✅ Font loads don't cause FOUT

---

## Future Enhancements

### Planned Features
- [ ] Search autocomplete with API integration
- [ ] Real provider data from backend
- [ ] User location detection
- [ ] Infinite scroll on search results
- [ ] Provider comparison tool
- [ ] Dark mode support
- [ ] i18n for multi-language support
- [ ] Advanced filtering (price range, distance, ratings)
- [ ] Save favorite providers
- [ ] Share providers on social media

### Technical Improvements
- [ ] Server-side rendering (SSR) for better SEO
- [ ] Progressive image loading (blur-up effect)
- [ ] Service worker for offline support
- [ ] Analytics integration
- [ ] A/B testing framework
- [ ] Accessibility audit (WCAG 2.1 AA)

---

## Troubleshooting

### Common Issues

**Video Not Playing:**
- Check video URL accessibility
- Verify `autoplay muted` attributes
- Check browser console for CORS errors
- Ensure poster image is set as fallback

**Images Not Loading:**
- Verify Unsplash URLs are accessible
- Check `handleImageError` function is called
- Test fallback mock image logic

**Font Not Applying:**
- Verify `fonts.css` is imported in `main.ts`
- Check font files exist in `/public/vazir-font-v16.1.0/`
- Inspect HTML `lang="fa"` attribute
- Clear browser cache

**RTL Issues:**
- Verify `dir="rtl"` on section element
- Check icon positioning (left vs right)
- Validate Persian text encoding (UTF-8)
- Test arrow direction in SVG paths

**Persian Numbers Not Converting:**
- Check `convertToPersianNumber` function
- Verify function is called in template
- Test with different number formats

---

## Related Documentation

- [README.md](./README.md) - Main frontend documentation
- [WEEK_7-8_FRONTEND_PLAN.md](./WEEK_7-8_FRONTEND_PLAN.md) - Frontend development plan
- [PERSIAN_TRANSLATIONS.md](./PERSIAN_TRANSLATIONS.md) - Translation reference
- [Provider Types](./src/modules/provider/types/provider.types.ts) - TypeScript types

---

## Credits

**Design Inspiration:**
- AsanRezerve (https://www.asanrezerve.com)
- Booking.com (https://www.booking.com)
- Airbnb (https://www.airbnb.com)

**Resources:**
- **Fonts:** Vazir v16.1.0 by Saber Rastikerdar
- **Icons:** Heroicons (https://heroicons.com)
- **Images:** Unsplash (https://unsplash.com)
- **Video:** Coverr (https://coverr.co)

**Contributors:**
- Frontend Development: Claude AI
- Design System: Based on modern booking platforms
- Persian Localization: Native Persian translations
