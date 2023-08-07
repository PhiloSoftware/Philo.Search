import type { Meta, StoryObj } from '@storybook/vue3';
// import vueRouter from 'storybook-vue3-router'
import Table from "./Table.vue";
import { ColumnFilterType, Comparator, FilterSet } from 'philo-search-core';
// import { DataColumn } from 'philo-search-core';

const meta: Meta<typeof Table> = {
  /* 👇 The title prop is optional.
   * See https://storybook.js.org/docs/vue/configure/overview#configure-story-loading
   * to learn how to generate automatic titles
   */
  title: 'Components/Table',
  component: Table,
  render: (args: any) => ({
    components: { Table },
    setup() {
      return {
        args
      };
    },
    template: '<Table v-bind="args" />',
  }),
  parameters: {
    // More on how to position stories at: https://storybook.js.org/docs/react/configure/story-layout
    layout: 'fullscreen',
  },
  // This component will have an automatically generated docsPage entry: https://storybook.js.org/docs/vue/writing-docs/autodocs
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof Table>;

export const Basic: Story = {
  args: {
    tableId: "yolo",
    page: 1,
    pageSize: 25,
    bindToQueryString: true,
    columns: [
      {
        field: "firstName",
        label: "First Name",
        visible: true,
        filter: {
          type: ColumnFilterType.text,
          action: Comparator.ILike,
          visible: true,
          defaultValues: '',
        },
      },
      {
        field: "lastName",
        label: "Last Name",
        visible: true,
        filter: {
          type: ColumnFilterType.text,
          action: Comparator.ILike,
          visible: true,
          defaultValues: '',
        },
      },
      {
        field: "age",
        label: "Age",
        visible: true,
        filter: {
          type: ColumnFilterType.number,
          defaultValues: '',
          action: Comparator.Eq,
          visible: true,
          props: {
            step: '5',
          },
        },
      },
      {
        field: "dob",
        label: "DOB",
        visible: true,
        filter: {
          type: ColumnFilterType.date,
          action: Comparator.Eq,
          defaultValues: '',
          visible: true,
          props: {
            min: "1900-01-01",
            max: "2021-12-03",
          },
        },
      },
      {
        field: "favAnimal",
        label: "Favourite Animal",
        visible: true,
        filter: {
          type: ColumnFilterType.text,
          defaultValues: [],
          action: Comparator.Eq,
          visible: true,
          props: {
            multiple: 'true',
          },
          options: [
            {
              value: "bee",
              label: "Bee",
            },
            {
              value: "cow",
              label: "Cow",
            },
            {
              value: "Dog",
              label: "Dog",
            },
          ],
        },
      },
    ],
    fetchRows: async (filter: FilterSet) => {
      await new Promise((resolve) => setTimeout(resolve, 2000));
      console.log(filter);
      const rows = [
        {
          firstName: "Billy",
          lastName: "Bob",
          age: 45,
        },
        {
          firstName: "Jane",
          lastName: "Doe",
          age: 22,
          dob: "2021-12-03",
        },
        {
          firstName: "Mary",
          lastName: "Jane",
          age: 64,
        },
        {
          firstName: "Ace",
          lastName: "Ventura",
          age: 64,
          favAnimal: 'bee'
        },
      ];
      return {
        rows: rows,
        totalRowCount: rows.length + 120,
      };
    }
  },
};