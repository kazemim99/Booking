import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

// This runs in Node.js - Don't use client-side code here (browser APIs, JSX...)

const config: Config = {
  title: 'Booksy Documentation',
  tagline: 'Complete guide for the Booksy booking platform',
  favicon: 'img/favicon.ico',

  // Future flags, see https://docusaurus.io/docs/api/docusaurus-config#future
  future: {
    v4: true, // Improve compatibility with the upcoming Docusaurus v4
  },

  // Set the production url of your site here
  url: 'https://docs.booksy.ir',
  // Set the /<baseUrl>/ pathname under which your site is served
  // For GitHub pages deployment, it is often '/<projectName>/'
  baseUrl: '/',

  // GitHub pages deployment config.
  // If you aren't using GitHub pages, you don't need these.
  organizationName: 'kazemim99', // Usually your GitHub org/user name.
  projectName: 'Booking', // Usually your repo name.

  onBrokenLinks: 'throw',

  // Even if you don't use internationalization, you can use this field to set
  // useful metadata like html lang. For example, if your site is Chinese, you
  // may want to replace "en" with "zh-Hans".
  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      {
        docs: {
          routeBasePath: '/',
          sidebarPath: './sidebars.ts',
          editUrl: 'https://github.com/kazemim99/Booking/tree/master/documentation/',
        },
        blog: false, // Disable blog for now
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  plugins: [
    [
      'docusaurus-plugin-openapi-docs',
      {
        id: 'openapi',
        docsPluginId: 'classic',
        config: {
          usermanagement: {
            specPath: 'static/openapi/usermanagement-v1.json',
            outputDir: 'docs/api/usermanagement',
            sidebarOptions: {
              groupPathsBy: 'tag',
              categoryLinkSource: 'tag',
            },
          },
          servicecatalog: {
            specPath: 'static/openapi/servicecatalog-v1.json',
            outputDir: 'docs/api/servicecatalog',
            sidebarOptions: {
              groupPathsBy: 'tag',
              categoryLinkSource: 'tag',
            },
          },
        },
      },
    ],
  ],

  themes: ['docusaurus-theme-openapi-docs'],

  themeConfig: {
    // Replace with your project's social card
    image: 'img/docusaurus-social-card.jpg',
    colorMode: {
      respectPrefersColorScheme: true,
    },
    navbar: {
      title: 'Booksy',
      logo: {
        alt: 'Booksy Logo',
        src: 'img/logo.svg',
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'tutorialSidebar',
          position: 'left',
          label: 'Documentation',
        },
        {
          to: '/api/usermanagement',
          label: 'UserManagement API',
          position: 'left',
        },
        {
          to: '/api/servicecatalog',
          label: 'ServiceCatalog API',
          position: 'left',
        },
        {
          href: 'https://github.com/kazemim99/Booking',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Documentation',
          items: [
            {
              label: 'Getting Started',
              to: '/intro',
            },
            {
              label: 'Architecture',
              to: '/architecture',
            },
          ],
        },
        {
          title: 'API Reference',
          items: [
            {
              label: 'UserManagement API',
              to: '/api/usermanagement',
            },
            {
              label: 'ServiceCatalog API',
              to: '/api/servicecatalog',
            },
          ],
        },
        {
          title: 'Resources',
          items: [
            {
              label: 'Postman Collection',
              href: '/Booksy_API_Collection.postman_collection.json',
            },
            {
              label: 'GitHub',
              href: 'https://github.com/kazemim99/Booking',
            },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} Booksy. Built with Docusaurus.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ['csharp', 'json', 'bash', 'typescript'],
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
