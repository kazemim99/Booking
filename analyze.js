const fs = require('fs');
const data = JSON.parse(fs.readFileSync('./ProvinceCity.json', 'utf8'));

console.log('Total records:', data.length);

const provinces = data.filter(x => x.CODEREC === 1);
console.log('Provinces (CODEREC=1):', provinces.length);

const cities = data.filter(x => x.CODEREC === 2);
console.log('Cities (CODEREC=2):', cities.length);

const others = data.filter(x => x.CODEREC !== 1 && x.CODEREC !== 2);
console.log('Others (CODEREC!=1,2):', others.length);

// Show sample province
console.log('\nSample Province:');
console.log(JSON.stringify(provinces[0], null, 2));

// Show sample city
console.log('\nSample City:');
console.log(JSON.stringify(cities[0], null, 2));

// Check unique CODEREC values
const uniqueCODEREC = [...new Set(data.map(x => x.CODEREC))];
console.log('\nUnique CODEREC values:', uniqueCODEREC.sort());
