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
  fetchRows: (filter, rowsReturned) => {
    console.log(filter);
    rowsReturned(
      [
        {
          col1: "Hey",
          col2: "Bro",
        },
      ],
      1
    );
  },
};
