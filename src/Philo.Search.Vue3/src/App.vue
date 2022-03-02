<script setup lang="ts">
import { FilterSet } from 'philo-search-core';
import { provide, ref } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import Table from './components/Table.vue'

var router = useRouter()
provide('router', () => router)
var route = useRoute();
provide('route', () => route);

const columns = ref([
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
  ])

const fetchRows = async (filter: FilterSet) => {
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
  }
</script>

<template>
  App Main
  <Table tableId="tbl" bind-to-query-string :columns="columns" :fetchRows="fetchRows" />
</template>

<style>
#app {
  font-family: Avenir, Helvetica, Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-align: center;
  color: #2c3e50;
  margin-top: 60px;
}
</style>
