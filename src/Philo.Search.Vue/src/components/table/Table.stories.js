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
  ],
  fetchRows: (filter) => {
    const rows = [
      {
        firstName: "Billy",
        lastName: "Bob",
      },
      {
        firstName: "Jane",
        lastName: "Doe",
      },
      {
        firstName: "Mary",
        lastName: "Jane",
      },
    ];
    return {
      rows: rows,
      totalRowCount: rows.length,
    };
  },
};
