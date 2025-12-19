import type {SidebarsConfig} from '@docusaurus/plugin-content-docs';

const sidebars: SidebarsConfig = {
  // Getting Started sidebar
  gettingStartedSidebar: [
    {
      type: 'category',
      label: 'Getting Started',
      items: [
        'getting-started/introduction',
        'getting-started/quick-start',
        'getting-started/advanced-setup',
        'getting-started/setup-complete',
      ],
    },
  ],

  // Architecture sidebar
  architectureSidebar: [
    {
      type: 'category',
      label: 'Architecture',
      items: [
        'architecture/overview',
        'architecture/cqrs-components',
        'architecture/business-requirements',
      ],
    },
    {
      type: 'category',
      label: 'Design Patterns',
      items: [
        {
          type: 'doc',
          id: 'architecture/overview',
          label: 'Domain-Driven Design',
        },
      ],
    },
  ],

  // Features sidebar
  featuresSidebar: [
    {
      type: 'category',
      label: 'Authentication',
      collapsed: false,
      items: [
        'features/authentication/authentication-flow',
        'features/authentication/quick-reference',
        'features/authentication/unified-auth',
        'features/authentication/fixes-summary',
      ],
    },
    {
      type: 'category',
      label: 'Booking Management',
      collapsed: false,
      items: [
        'features/booking/cancellation',
        'features/booking/rescheduling',
        'features/booking/integration',
        'features/booking/realtime-availability',
      ],
    },
    {
      type: 'category',
      label: 'Provider Management',
      collapsed: false,
      items: [
        'features/provider/profile-api',
        'features/provider/search-guide',
        'features/provider/access-ux',
        'features/provider/hierarchy-mvp',
      ],
    },
  ],

  // Deployment sidebar
  deploymentSidebar: [
    {
      type: 'category',
      label: 'Deployment',
      items: [
        'deployment/overview',
        'deployment/docker-compose',
        'deployment/database-setup',
      ],
    },
    {
      type: 'category',
      label: 'Testing',
      items: [
        'testing/integration-testing',
        'testing/reqnroll-quickstart',
        'testing/test-coverage',
        'testing/quick-guide',
      ],
    },
    {
      type: 'category',
      label: 'Implementation Tracking',
      items: [
        'implementation/status',
        'implementation/summary',
        'changelog/changelog',
      ],
    },
  ],
};

export default sidebars;
