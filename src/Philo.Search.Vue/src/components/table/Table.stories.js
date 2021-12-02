import Table from "./Table.vue";
import { FilterSet } from "@/processor/datastructure";

export default {
  component: Table,
  title: "Components/Table",
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
  ></Table>`,
});

export const basic = Template.bind({});
basic.args = {
  tableId: "yolo",
  page: 1,
  pageSize: 20,
  columns: [
    {
      field: "firstName",
      label: "First Name",
      visible: true,
      filter: {
        type: "text",
        value: "yo",
        action: "Like",
      },
    },
    {
      field: "lastName",
      label: "Last Name",
      visible: true,
      filter: {
        type: "text",
        value: "yo",
        action: "Like",
      },
    },
    {
      field: "age",
      label: "Age",
      visible: true,
      filter: {
        type: "number",
        value: "3",
        action: "Like",
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
        value: "3",
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
  fetchRows: (filter) => {
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
      },
      {
        firstName: "Mary",
        lastName: "Jane",
        age: 64,
      },
    ];
    return {
      rows: rows,
      totalRowCount: rows.length,
    };
  },
};
