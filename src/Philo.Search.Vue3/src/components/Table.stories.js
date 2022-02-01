import vueRouter from 'storybook-vue3-router'
import Table from "./Table.vue";
import { DataColumn } from 'philo-search-core';

export default {
  component: Table,
  title: "Components/Table",
  decorators: [vueRouter()],
  args: {
    tableId: 'hello'
  },
  argTypes: {
    tableId: {
      control: ""
    }
  }
};

const Template = (args, { argTypes }) => ({
  components: { Table },
  setup() {
    //👇 The args will now be passed down to the template
    return { args };
  },
  template: `<Table v-bind="args">
    <template v-slot:cell-firstName="{ row, value }">
      <b>{{ value }}</b>
    </template>
  </Table>`,
});

export const basic = Template.bind({});
basic.args = {
  tableId: "yolo",
  page: 1,
  pageSize: 25,
  bindToQueryString: true,
  rowClickable: true,
  columns: [
    {
      field: "firstName",
      label: "First Name",
      visible: true,
      filter: {
        type: "text",
        action: "Like",
        visible: true,
        defaultValues: '',
      },
    },
    {
      field: "lastName",
      label: "Last Name",
      visible: true,
      filter: {
        type: "text",
        action: "Like",
        visible: true,
        defaultValues: '',
      },
    },
    {
      field: "age",
      label: "Age",
      visible: true,
      filter: {
        type: "number",
        defaultValues: '',
        action: "Like",
        visible: true,
        props: {
          step: 5,
        },
      },
    },
    {
      field: "dob",
      label: "DOB",
      visible: true,
      filter: {
        type: "date",
        action: "Eq",
        defaultValues: '',
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
        type: "text",
        defaultValues: [],
        action: "Eq",
        props: {
          multiple: true,
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
  fetchRows: async (filter) => {
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
      totalRowCount: rows.length + 20,
    };
  },
};
