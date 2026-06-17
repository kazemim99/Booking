/**
 * Coliride-styled Ant Design theme for the Booksy admin.
 *
 * Maps the shared Booksy/Coliride brand tokens onto Ant Design Vue's GLOBAL theme tokens so
 * the whole dashboard inherits the brand without restyling each component. We intentionally use
 * only global/seed tokens (ant-design-vue 4.x leaves most per-component ComponentToken
 * interfaces empty, so component-level keys are unsupported here). Per-component fine-tuning is
 * done with small CSS overrides instead. Keep in sync with booksy-frontend's design-tokens.scss
 * (both will be unified into a shared @booksy/tokens package).
 */
import type { ThemeConfig } from 'ant-design-vue/es/config-provider/context'

// Coliride brand
const NAVY = '#4d5e80' // dominant ink
const BLUE = '#3777bf' // primary / actions
const BLUE_CHROME = '#3777c0' // app bar / nav
const GREEN = '#0ac075' // success / accent
const RED = '#ff6171' // error
const GOLD = '#ffcb33' // warning / rating

const FONT_STACK =
  "'Vazirmatn', 'Vazir', 'Poppins', system-ui, -apple-system, 'Segoe UI', sans-serif"

export const antdTheme: ThemeConfig = {
  token: {
    colorPrimary: BLUE,
    colorInfo: BLUE_CHROME,
    colorSuccess: GREEN,
    colorError: RED,
    colorWarning: GOLD,

    colorTextBase: NAVY,
    colorText: NAVY,
    colorTextSecondary: '#96a0b3',
    colorTextTertiary: '#c3cad9',

    colorBorder: '#e5e9f2',
    colorBorderSecondary: '#ebeef3',
    colorBgLayout: '#f5f5f5',
    colorBgContainer: '#ffffff',

    // Coliride's soft 10–16px corners
    borderRadius: 10,
    borderRadiusLG: 16,
    borderRadiusSM: 8,

    fontFamily: FONT_STACK,

    // A touch airier, closer to Coliride's generous controls
    controlHeight: 40,

    // Coliride is flat — soften Ant's default elevation
    boxShadow: '0 2px 8px 0 rgba(77, 94, 128, 0.08)',
    boxShadowSecondary: '0 6px 20px -4px rgba(77, 94, 128, 0.10)',
    wireframe: false,
  },
}

export default antdTheme
