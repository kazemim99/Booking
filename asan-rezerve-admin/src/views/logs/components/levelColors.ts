import type { CategoryLevel, LogLevelName } from '../../../api/observability.api'

/** Tag colours per level; errors stand out, chatter stays quiet. */
export const LEVEL_COLORS: Record<LogLevelName | CategoryLevel, string> = {
  Verbose: 'default',
  Trace: 'default',
  Debug: 'purple',
  Information: 'blue',
  Warning: 'orange',
  Error: 'red',
  Fatal: 'magenta',
  Critical: 'magenta',
  None: 'default',
}
