<script setup lang="ts">
import { ref } from 'vue';

const history = ref<any>([
  { a: 1, b: 2, operation: '+', result: 3 },
  { a: 3, b: 2, operation: '-', result: 1 },
  { a: 4, b: 2, operation: '*', result: 8 },
  { a: 6, b: 2, operation: '/', result: 3 },
]);

const a = ref<number | null>(null);
const b = ref<number | null>(null);
const operation = ref<string | null>(null);

const calculate = () => { //TODO: API call
  if (a.value === null
      || b.value === null
      || operation.value === null) {
    return;
  }

  const problem = {
    operandA: a.value,
    operandB: b.value,
  };

  let result = null;

  if (operation.value === '+') {
    //Send an HTTP request with problem as the body
    fetch('http://sum-service:80/Sum', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(problem),
    }).then(response => response.json())
      .then(data => {
        console.log('Success:', data);
        result = data.value; // { value: 3 }
      })
      .catch((error) => {
        console.error('Error:', error);
      });
  } else {
    console.log('Operand not supported');
  }

  console.log(result);

  history.value.unshift({
    a: a.value,
    b: b.value,
    operation: operation.value,
    result: result,
  });

  a.value = null;
  b.value = null;
};

</script>

<template>
  <header>

  </header>

  <main>
    <h1>Calculator</h1>
    <div>
      <input type="number" v-model="a" />
      <select v-model="operation">
        <option value="+">+</option>
        <option value="-">-</option>
        <option value="*">*</option>
        <option value="/">/</option>
      </select>
      <input type="number" v-model="b" />
      <button @click="calculate">Calculate</button>
    </div>

    <h1>History</h1>
    <p v-for="item in history" :key="item.id">
      {{ item.a }} {{ item.operation }} {{ item.b }} = {{ item.result }}
    </p>
  </main>
</template>

<style scoped>

</style>
