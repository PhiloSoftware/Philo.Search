import Table from "./Table.vue";
import StoryRouter from '../router'

export default {
  component: Table,
  title: "Components/Table",
  decorators: [StoryRouter()]
};

const Template = (args, { argTypes }) => ({
  props: Object.keys(argTypes),
  components: { Table },
  template: `<Table
    :tableId='tableId'
    :columns='columns'
    :page='page'
    :pageSize='pageSize'
    :fetchRows='fetchRows'
    :bindToQueryString='bindToQueryString'
    :rowClickable="rowClickable"
  >
    <template v-slot:cell='{ col, row, value }'>
      {{ value }}
    </template>
    <template v-slot:field-text='{ filter, change }'>
      <div>
        {{filter.label}}
      </div>  
      <div>
        <input v-model="filter.value" @keyup="change" />
      </div>
    </template>
  </Table>`,
});

export const basic = Template.bind({});

basic.args = {
  tableId: "yolo",
  page: 1,
  pageSize: 10,
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
        value: [],
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
    ];
    return {
      rows: rows,
      totalRowCount: 20,
    };
  },
};
